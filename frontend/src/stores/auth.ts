import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User, AuthResponse, LoginRequest, RegisterRequest } from '@/types/auth'
import { authAPI } from '@/utils/api'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const accessToken = ref<string | null>(localStorage.getItem('accessToken'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))

  const isAuthenticated = computed(() => !!accessToken.value && !!user.value)

  const login = async (loginData: LoginRequest): Promise<void> => {
    try {
      const response = await authAPI.login(loginData)
      setAuthData(response)
    } catch (error) {
      throw error
    }
  }

  const register = async (registerData: RegisterRequest): Promise<void> => {
    try {
      const response = await authAPI.register(registerData)
      setAuthData(response)
    } catch (error) {
      throw error
    }
  }

  const logout = () => {
    user.value = null
    accessToken.value = null
    refreshToken.value = null
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('user')
  }

  const setAuthData = (authResponse: AuthResponse) => {
    user.value = authResponse.user
    accessToken.value = authResponse.accessToken
    refreshToken.value = authResponse.refreshToken
    
    localStorage.setItem('accessToken', authResponse.accessToken)
    localStorage.setItem('refreshToken', authResponse.refreshToken)
    localStorage.setItem('user', JSON.stringify(authResponse.user))
  }

  const initializeAuth = () => {
    const storedUser = localStorage.getItem('user')
    if (storedUser && accessToken.value) {
      user.value = JSON.parse(storedUser)
    }
  }

  const refreshAccessToken = async (): Promise<boolean> => {
    try {
      if (!refreshToken.value) return false
      
      const response = await authAPI.refresh({ refreshToken: refreshToken.value })
      setAuthData(response)
      return true
    } catch (error) {
      logout()
      return false
    }
  }

  return {
    user,
    accessToken,
    refreshToken,
    isAuthenticated,
    login,
    register,
    logout,
    initializeAuth,
    refreshAccessToken
  }
})