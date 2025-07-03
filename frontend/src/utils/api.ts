import axios, { type AxiosResponse } from 'axios'
import type { AuthResponse, LoginRequest, RegisterRequest } from '@/types/auth'
import { useAuthStore } from '@/stores/auth'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
})

// Request interceptor to add auth token
api.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  if (authStore.accessToken) {
    config.headers.Authorization = `Bearer ${authStore.accessToken}`
  }
  return config
})

// Response interceptor to handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const authStore = useAuthStore()
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      
      const refreshed = await authStore.refreshAccessToken()
      if (refreshed) {
        originalRequest.headers.Authorization = `Bearer ${authStore.accessToken}`
        return api(originalRequest)
      } else {
        authStore.logout()
        window.location.href = '/login'
      }
    }

    return Promise.reject(error)
  }
)

export const authAPI = {
  login: (data: LoginRequest): Promise<AuthResponse> =>
    api.post('/auth/login', data).then((res: AxiosResponse<AuthResponse>) => res.data),
  
  register: (data: RegisterRequest): Promise<AuthResponse> =>
    api.post('/auth/register', data).then((res: AxiosResponse<AuthResponse>) => res.data),
  
  refresh: (data: { refreshToken: string }): Promise<AuthResponse> =>
    api.post('/auth/refresh', data).then((res: AxiosResponse<AuthResponse>) => res.data),
  
  me: () =>
    api.get('/auth/me').then((res) => res.data),
}

export const systemAPI = {
  getSystems: () =>
    api.get('/systems').then((res) => res.data),
  
  createSystem: (data: any) =>
    api.post('/systems', data).then((res) => res.data),
  
  getSystem: (id: string) =>
    api.get(`/systems/${id}`).then((res) => res.data),
  
  updateSystem: (id: string, data: any) =>
    api.put(`/systems/${id}`, data).then((res) => res.data),
  
  deleteSystem: (id: string) =>
    api.delete(`/systems/${id}`).then((res) => res.data),
}

export const repositoryAPI = {
  getRepositories: (systemId: string) =>
    api.get(`/systems/${systemId}/repositories`).then((res) => res.data),
  
  addRepository: (systemId: string, data: any) =>
    api.post(`/systems/${systemId}/repositories`, data).then((res) => res.data),
  
  updateRepository: (systemId: string, id: string, data: any) =>
    api.put(`/systems/${systemId}/repositories/${id}`, data).then((res) => res.data),
  
  deleteRepository: (systemId: string, id: string) =>
    api.delete(`/systems/${systemId}/repositories/${id}`).then((res) => res.data),
}

export default api