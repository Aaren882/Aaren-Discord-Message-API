<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'

// --- State Management ---
const API_BASE_URL = 'http://localhost:5000' // Adjust to your Arma3WebService URL
const isConnected = ref(false)
const isSending = ref(false)
const activeMissions = ref<any[]>([])
const availableMessageIds = ref<string[]>([])
const searchQuery = ref('')

const messageLog = ref([
  { id: Date.now(), time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }), status: 'info', text: 'Dashboard initialized. Attempting to connect...' },
])

const testMessage = ref('')
const templateMessageId = ref('')
const templateFields = ref({
  content: 'Server Status Update',
  title: '🛰️ {name}',
  description: '**Mission:** {mission}\n**Players:** {playerCount}/{maxPlayers}\n**Uptime:** {uptime}',
  color: '#5865f2',
  footer: 'Last update: {time}'
})
const isUpdatingTemplate = ref(false)

let pollInterval: number | undefined

const filteredMissions = computed(() => {
  const query = searchQuery.value.toLowerCase().trim()
  if (!query) return activeMissions.value
  return activeMissions.value.filter(server =>
    (server.name?.toLowerCase().includes(query)) ||
    (server.mission?.toLowerCase().includes(query))
  )
})

const addLog = (status: 'success' | 'info' | 'error', text: string) => {
  messageLog.value.unshift({
    id: Date.now(),
    time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    status,
    text
  })
  if (messageLog.value.length > 50) messageLog.value.pop()
}

// --- Logic ---
async function fetchAvailableMessageIds() {
  try {
    const response = await fetch(`${API_BASE_URL}/available-message-ids`)
    if (response.ok) {
      const data = await response.json()
      availableMessageIds.value = data // Expecting an array of strings
      // If no templateMessageId is set yet, and we have available IDs, pick the first one
      if (!templateMessageId.value && availableMessageIds.value.length > 0) {
        templateMessageId.value = availableMessageIds.value[0]
      }
    } else {
      addLog('error', 'Failed to fetch available message IDs from backend.')
    }
  } catch (err) {
    addLog('error', `Error fetching available message IDs: ${err instanceof Error ? err.message : 'Unknown error'}`)
  }
}

async function fetchTemplate() {
  try {
    const response = await fetch(`${API_BASE_URL}/template`)
    if (response.ok) {
      const data = await response.json()
      if (data.messageId) {
        templateMessageId.value = data.messageId
      } else if (availableMessageIds.value.length > 0) { // If no specific ID from backend, but we have options
        templateMessageId.value = availableMessageIds.value[0]
      }
      if (data.template) { // Sync the form fields if the backend provides the current template configuration
        templateFields.value = { ...templateFields.value, ...data.template }
      }
    }
  } catch (err) {
    addLog('error', 'Failed to fetch existing template configuration from backend')
  }
}

async function fetchStatus() {
  try {
    const response = await fetch(`${API_BASE_URL}/status`)
    if (!response.ok) throw new Error('Service Unreachable')

    const data = await response.json()
    activeMissions.value = Array.isArray(data) ? data : [data]
    if (!isConnected.value) {
      isConnected.value = true
      addLog('success', 'Connected to Arma3WebService')
    }
  } catch {
    if (isConnected.value) {
      isConnected.value = false
      addLog('error', 'Lost connection to backend service')
    }
  }
}

async function sendTestMessage () {
  if (!testMessage.value || isSending.value) return

  isSending.value = true
  try {
    const response = await fetch(`${API_BASE_URL}/send`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ message: testMessage.value })
    })

    if (response.ok) {
      addLog('success', `Sent to Discord: ${testMessage.value}`)
      testMessage.value = ''
    } else {
      throw new Error('Failed to send message')
    }
  } catch (err) {
    addLog('error', `Delivery failed: ${err instanceof Error ? err.message : 'Unknown error'}`)
  } finally {
    isSending.value = false
  }
}

async function updateTemplate() {
  if (!templateMessageId.value || isUpdatingTemplate.value) return

  // Construct Discord Message JSON from structured form fields
  const colorInt = parseInt(templateFields.value.color.replace('#', ''), 16)

  const payload = {
    content: templateFields.value.content,
    embeds: [{
      title: templateFields.value.title,
      description: templateFields.value.description,
      color: colorInt,
      footer: {
        text: templateFields.value.footer
      }
    }]
  }

  isUpdatingTemplate.value = true
  try {
    const response = await fetch(`${API_BASE_URL}/update-template`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        messageId: templateMessageId.value,
        template: payload
      })
    })

    if (response.ok) {
      addLog('success', `Template updated for message ID: ${templateMessageId.value}.`)
    } else {
      throw new Error('Failed to update template')
    }
  } catch (err) {
    addLog('error', `Template update failed: ${err instanceof Error ? err.message : 'Unknown error'}`)
  } finally {
    isUpdatingTemplate.value = false
  }
}

onMounted(() => {
  fetchStatus()
  fetchAvailableMessageIds() // Fetch available IDs first
  fetchTemplate()
  pollInterval = window.setInterval(fetchStatus, 5000) // Poll every 5s
})

onUnmounted(() => {
  if (pollInterval) clearInterval(pollInterval)
})
</script>

<template>
  <div class="dashboard-container">
    <header class="app-header">
      <div class="header-left">
        <img alt="Service Logo" class="logo" src="./assets/logo.svg" width="40" height="40" />
        <h1>Arma3WebService <span class="version">v1.0</span></h1>
      </div>
      <div class="status-badge" :class="{ connected: isConnected }">
        {{ isConnected ? 'CONNECTED' : 'DISCONNECTED' }}
      </div>
    </header>

    <main class="dashboard-grid">
      <!-- Search Filter -->
      <div class="search-container">
        <input
          v-model="searchQuery"
          placeholder="Filter missions by name..."
          class="search-input"
        />
      </div>

      <!-- Active Missions -->
      <section v-for="(server, index) in filteredMissions" :key="index" class="card stats-card">
        <h2>{{ server.name || 'Mission Instance' }}</h2>
        <div class="stats-grid">
          <div class="stat-item">
            <label>Server Name</label>
            <p>{{ server.name }}</p>
          </div>
          <div class="stat-item">
            <label>Current Mission</label>
            <p>{{ server.mission }}</p>
          </div>
          <div class="stat-item">
            <label>Players</label>
            <p>{{ server.playerCount }} / {{ server.maxPlayers }}</p>
          </div>
          <div class="stat-item">
            <label>Uptime</label>
            <p>{{ server.uptime }}</p>
          </div>
        </div>
      </section>

      <!-- Status Placeholder -->
      <section v-if="filteredMissions.length === 0" class="card stats-card">
        <h2>Status</h2>
        <p v-if="activeMissions.length === 0">{{ isConnected ? 'No missions currently active.' : 'Connecting to service...' }}</p>
        <p v-else>No results matching "{{ searchQuery }}".</p>
      </section>

      <!-- Control Panel -->
      <section class="card control-card">
        <h2>Quick Actions</h2>
        <div class="input-group">
          <input
            v-model="testMessage"
            placeholder="Type a message to send to Discord..."
            @keyup.enter="sendTestMessage"
          />
          <button @click="sendTestMessage" :disabled="!isConnected">Send</button>
        </div>
      </section>

      <!-- Template Update Panel -->
      <section class="card template-card">
        <h2>Update Server Info Template</h2>
        <p class="card-description">Configure the JSON template for Discord messages that display server information. Use a Discord message ID to target a specific message for updates.</p>

        <form class="template-form" @submit.prevent="updateTemplate">
          <div class="input-field">
            <label>Target Message ID</label>
            <select v-model="templateMessageId" :disabled="availableMessageIds.length === 0">
              <option v-if="availableMessageIds.length === 0" value="" disabled>
                {{ isConnected ? 'No message IDs available' : 'Connecting...' }}
              </option>
              <option v-for="id in availableMessageIds" :key="id" :value="id">
                {{ id }}
              </option>
            </select>
          </div>

          <div class="form-row">
            <div class="input-field flex-2">
              <label>Outer Message Text</label>
              <input v-model="templateFields.content" placeholder="Text above the embed..." />
            </div>
            <div class="input-field flex-1">
              <label>Embed Color</label>
              <input type="color" v-model="templateFields.color" class="color-picker" />
            </div>
          </div>

          <div class="input-field">
            <label>Embed Title</label>
            <input v-model="templateFields.title" placeholder="e.g. {name} - Status" />
          </div>

          <div class="input-field">
            <label>Embed Description (Placeholders: {name}, {mission}, {playerCount}, {maxPlayers}, {uptime})</label>
            <textarea v-model="templateFields.description" rows="4" spellcheck="false"></textarea>
          </div>

          <div class="input-field">
            <label>Footer Text (Placeholder: {time})</label>
            <input v-model="templateFields.footer" placeholder="e.g. Updated at {time}" />
          </div>

          <button type="submit" :disabled="!isConnected || isUpdatingTemplate">
            {{ isUpdatingTemplate ? 'Updating...' : 'Update Template' }}
          </button>
        </form>

      </section>

      <!-- Activity Log -->
      <section class="card log-card">
        <h2>Recent Activity</h2>
        <div class="log-container">
          <div v-for="log in messageLog" :key="log.id" class="log-entry">
            <span class="log-time">[{{ log.time }}]</span>
            <span class="log-text" :class="log.status">{{ log.text }}</span>
          </div>
        </div>
      </section>
    </main>
  </div>
</template>

<style scoped>
.dashboard-container {
  min-height: 100vh;
  padding: 2rem;
  background-color: #1a1b1e;
  color: #c1c2c5;
  font-family: 'Inter', sans-serif;
}

.app-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  border-bottom: 1px solid #2c2e33;
  padding-bottom: 1rem;
}

.header-left { display: flex; align-items: center; gap: 1rem; }
.header-left h1 { font-size: 1.5rem; margin: 0; color: #fff; }
.version { font-size: 0.8rem; color: #5c5f66; font-weight: normal; }

.status-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: bold;
  background: #fa5252;
  color: white;
}
.status-badge.connected { background: #40c057; }

.dashboard-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
}

.card {
  background: #25262b;
  border-radius: 8px;
  padding: 1.5rem;
  border: 1px solid #2c2e33;
}

.card-description { font-size: 0.85rem; color: #909296; margin-top: -1rem; margin-bottom: 1.5rem; line-height: 1.4; }

.card h2 { font-size: 1rem; margin-top: 0; margin-bottom: 1.5rem; color: #909296; text-transform: uppercase; letter-spacing: 1px; }

.stats-grid { display: grid; grid-template-columns: repeat(2, 1fr); gap: 1rem; }
.stat-item label { display: block; font-size: 0.75rem; color: #5c5f66; margin-bottom: 0.25rem; }
.stat-item p { margin: 0; font-weight: 600; color: #e9ecef; }

.form-row { display: flex; gap: 1rem; }
.flex-1 { flex: 1; }
.flex-2 { flex: 2; }

.input-group { display: flex; gap: 0.5rem; }

input, textarea, select {
  flex: 1;
  background: #141517;
  border: 1px solid #373a40;
  border-radius: 4px;
  padding: 0.75rem 1rem;
  color: white;
  color-scheme: dark;
  font: inherit;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}

input:focus, textarea:focus, select:focus {
  border-color: #5865f2;
  box-shadow: 0 0 0 3px rgba(88, 101, 242, 0.15);
}

select {
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24' fill='none' stroke='%23909296' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpolyline points='6 9 12 15 18 9'%3E%3C/polyline%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 0.8rem center;
  background-size: 1.1rem;
  padding-right: 2.5rem;
  cursor: pointer;
}

select:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.color-picker { padding: 2px; height: 42px; cursor: pointer; }

label { display: block; font-size: 0.75rem; color: #5c5f66; margin-bottom: 0.25rem; text-transform: uppercase; letter-spacing: 0.5px; }

button {
  background: #5865f2;
  border: none;
  border-radius: 4px;
  color: white;
  padding: 0 1rem;
  cursor: pointer;
}
button:disabled { background: #2c2e33; cursor: not-allowed; }

.log-card { grid-column: span 2; }
.log-container {
  height: 150px;
  overflow-y: auto;
  background: #141517;
  padding: 1rem;
  border-radius: 4px;
  font-family: 'Courier New', monospace;
  font-size: 0.9rem;
}

.log-entry { margin-bottom: 0.5rem; border-bottom: 1px solid #2c2e33; padding-bottom: 0.25rem; }
.log-time { color: #5c5f66; margin-right: 0.5rem; }
.log-text.success { color: #40c057; }
.log-text.info { color: #339af0; }

@media (max-width: 768px) {
  .dashboard-grid { grid-template-columns: 1fr; }
  .log-card { grid-column: auto; }
}
</style>
