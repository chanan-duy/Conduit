import { useAuth } from '@/composables/useAuth'
import type { AxiosInstance, InternalAxiosRequestConfig } from 'axios'
import axios, { isAxiosError } from 'axios'
import { shallowRef } from 'vue'

type RegisterRequestDto = { email: string; password: string; displayName?: string }
type LoginRequestDto = { email: string; password: string }
type LoginResponseDto = { accessToken: string; expiresIn: number; refreshToken: string }
type RefreshRequestDto = { refreshToken: string }
type RefreshResponseDto = { accessToken: string; expiresIn: number; refreshToken: string }
type ExchangeRequestDto = { exchangeToken: string; meetingId: string }
type ExchangeResponseDto = { authToken: string; expiresIn: number }
type RenewResult = 'renewed' | 'unauthorized' | 'failed'

export class AuthService {
  private readonly ax: AxiosInstance
  private readonly LocalStorageKeyToken = '__accessToken'
  private readonly LocalStorageKeyTokenRenew = '__accessTokenRenew'
  private readonly LocalStorageKeyTokenExpires = '__accessTokenExpires'

  public isLoggedIn = shallowRef<boolean>(false)
  private _renewTokenPromise: Promise<boolean> | null = null

  constructor() {
    this.ax = axios.create({
      baseURL: '/api/v1/auth',
    })
    this.init()
  }

  private init = (): void => {
    this.isLoggedIn.value = !!this.getAccessToken() && !this.isAccessTokenExpired()
  }

  public login = async (dto: LoginRequestDto): Promise<LoginResponseDto> => {
    const { data } = await this.ax.post<LoginResponseDto>('/login', dto)
    this.setTokens(data)
    this.isLoggedIn.value = true
    return data
  }

  public logout = async (): Promise<void> => {
    const refreshToken = this.getRenewToken()
    this.removeTokens()
    this.isLoggedIn.value = false

    if (refreshToken) {
      try {
        await this.ax.post('/logout', { refreshToken } satisfies RefreshRequestDto)
      } catch (error) {
        console.log('logout request failed', error)
      }
    }
  }

  public register = async (dto: RegisterRequestDto): Promise<void> => {
    await this.ax.post('/register', dto)
  }

  public exchangeToken = async (dto: ExchangeRequestDto): Promise<void> => {
    const { data } = await this.ax.post<ExchangeResponseDto>(`/exchange-token`, dto)
    this.setTokens({ accessToken: data.authToken, expiresIn: data.expiresIn, refreshToken: '' })
  }

  public renewToken = (): Promise<boolean> => {
    if (!this._renewTokenPromise) {
      const rejectedToken = this.getAccessToken()
      this._renewTokenPromise = this.refreshAcrossTabs(rejectedToken)
        .then((result) => {
          if (result === 'unauthorized') return this.handleAuthenticationFailure()
          return result === 'renewed'
        })
        .finally(() => {
          this._renewTokenPromise = null
        })
    }
    return this._renewTokenPromise
  }

  private refreshAcrossTabs = async (rejectedToken: string | null): Promise<RenewResult> => {
    if (!navigator.locks) return this.refresh(rejectedToken)
    return await navigator.locks.request('conduit-token-refresh', () => this.refresh(rejectedToken))
  }

  private refresh = async (rejectedToken: string | null): Promise<RenewResult> => {
    if (this.getAccessToken() !== rejectedToken && !this.isAccessTokenExpired()) {
      this.isLoggedIn.value = true
      return 'renewed'
    }

    const refreshToken = this.getRenewToken()
    if (!refreshToken) return 'unauthorized'

    try {
      const dto = { refreshToken } satisfies RefreshRequestDto
      const { data } = await this.ax.post<RefreshResponseDto>('/refresh', dto)
      this.setTokens(data)
      this.isLoggedIn.value = true
      return 'renewed'
    } catch (error) {
      if (isAxiosError(error) && error.response?.status === 401) {
        this.removeTokens()
        this.isLoggedIn.value = false
        return 'unauthorized'
      }

      console.log('unhandled error in renew', error)
      return 'failed'
    }
  }

  public getVerifiedAccessToken = async (): Promise<string | null> => {
    const token = this.getAccessToken()

    if (!token || this.isAccessTokenExpired()) {
      const success = await this.renewToken()
      return success ? this.getAccessToken() : null
    }
    return token
  }

  public addAxiosInterceptor = (axi: AxiosInstance): void => {
    axi.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
      if (config.baseURL === this.ax.defaults.baseURL) return config

      const token = await this.getVerifiedAccessToken()
      if (token) {
        config.headers.Authorization = `Bearer ${token}`
      }
      return config
    })

    axi.interceptors.response.use(undefined, async (error) => {
      const originalConfig = error.config
      const isAuthError = error.response?.status === 401
      const canRetry = !originalConfig._retry && originalConfig.baseURL !== this.ax.defaults.baseURL

      if (isAuthError && canRetry) {
        originalConfig._retry = true
        if (await this.renewToken()) {
          originalConfig.headers.Authorization = `Bearer ${this.getAccessToken()}`
          return axi(originalConfig)
        }
      }
      return Promise.reject(error)
    })
  }

  public getUserId = (): string | null => {
    const token = this.getAccessToken()
    if (!token) return null

    try {
      const payload = token.split('.')[1]!.replace(/-/g, '+').replace(/_/g, '/')
      return (JSON.parse(atob(payload)) as { sub?: string }).sub ?? null
    } catch {
      return null
    }
  }

  private handleAuthenticationFailure = (): Promise<boolean> => {
    return useAuth().showLoginModal()
  }

  private getAccessToken = (): string | null => {
    return localStorage.getItem(this.LocalStorageKeyToken)
  }

  private getRenewToken = (): string | null => {
    return localStorage.getItem(this.LocalStorageKeyTokenRenew)
  }

  private isAccessTokenExpired = (): boolean => {
    const expires = localStorage.getItem(this.LocalStorageKeyTokenExpires)
    return !expires || Date.now() > parseInt(expires, 10)
  }

  private setTokens = (tokens: {
    accessToken: string
    refreshToken: string
    expiresIn: number
  }): void => {
    const expires = Date.now() + tokens.expiresIn * 1000
    localStorage.setItem(this.LocalStorageKeyToken, tokens.accessToken)
    localStorage.setItem(this.LocalStorageKeyTokenRenew, tokens.refreshToken)
    localStorage.setItem(this.LocalStorageKeyTokenExpires, expires.toString())
  }

  private removeTokens = (): void => {
    localStorage.removeItem(this.LocalStorageKeyToken)
    localStorage.removeItem(this.LocalStorageKeyTokenRenew)
    localStorage.removeItem(this.LocalStorageKeyTokenExpires)
  }
}

export const auth = new AuthService()
