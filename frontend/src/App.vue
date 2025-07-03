<template>
  <div id="app">
    <el-container class="app-container">
      <el-header v-if="isAuthenticated" class="app-header">
        <AppHeader />
      </el-header>
      
      <el-container>
        <el-aside v-if="isAuthenticated" class="app-sidebar">
          <AppSidebar />
        </el-aside>
        
        <el-main class="app-main">
          <router-view />
        </el-main>
      </el-container>
    </el-container>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import AppHeader from '@/components/layout/AppHeader.vue'
import AppSidebar from '@/components/layout/AppSidebar.vue'

const authStore = useAuthStore()

const isAuthenticated = computed(() => authStore.isAuthenticated)
</script>

<style scoped>
.app-container {
  height: 100vh;
}

.app-header {
  background-color: #409eff;
  color: white;
  display: flex;
  align-items: center;
  padding: 0 20px;
}

.app-sidebar {
  width: 250px;
  background-color: #f5f5f5;
  border-right: 1px solid #e0e0e0;
}

.app-main {
  padding: 20px;
  background-color: #fafafa;
}
</style>