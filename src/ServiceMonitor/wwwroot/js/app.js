            // ServiceMonitor Web UI - Main Script

let currentConfig = null;

// DOM Elements
const loadingEl = document.getElementById('loading');
const errorEl = document.getElementById('error');
const configForm = document.getElementById('configForm');
const addUrlBtn = document.getElementById('addUrlBtn');
const addRecipientBtn = document.getElementById('addRecipientBtn');
const cancelBtn = document.getElementById('cancelBtn');

// Initialization
document.addEventListener('DOMContentLoaded', async () => {
    await loadConfig();
    setupEventListeners();
});

// Setup Event Listeners
function setupEventListeners() {
    configForm.addEventListener('submit', handleSubmit);
    addUrlBtn.addEventListener('click', () => addUrlField());
    addRecipientBtn.addEventListener('click', () => addRecipientField());
    cancelBtn.addEventListener('click', () => loadConfig());
}

// Load Configuration
async function loadConfig() {
    try {
        showLoading(true);
        hideError();

        const response = await fetch('/api/config');
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        currentConfig = await response.json();
        populateForm(currentConfig);
        
        showLoading(false);
        configForm.style.display = 'block';
    } catch (error) {
        showError(`Error loading configuration: ${error.message}`);
        showLoading(false);
    }
}

// Populate Form with Data
function populateForm(config) {
    // System Settings
    if (config.system) {
        document.getElementById('timeoutSeconds').value = config.system.timeoutSeconds || 30;           
        document.getElementById('daemonIntervalMinutes').value = config.system.daemonIntervalMinutes || 60;
        document.getElementById('webUiPort').value = config.system.webUiPort || 8080;
        document.getElementById('runMode').value = config.system.runMode || 'Once';
    }

    // URLs
    const urlsList = document.getElementById('urlsList');
    urlsList.innerHTML = '';
    if (config.urls && config.urls.length > 0) {
        config.urls.forEach(url => addUrlField(url));
    } else {
        addUrlField();
    }

    // Email Server
    if (config.emailServer) {
        document.getElementById('smtpHost').value = config.emailServer.host || '';
        document.getElementById('smtpPort').value = config.emailServer.port || 465;
        document.getElementById('smtpUsername').value = config.emailServer.user || '';
        document.getElementById('smtpPassword').value = config.emailServer.password || '';
        document.getElementById('smtpFromEmail').value = config.emailServer.defaultEmailSenderAddress || '';
        document.getElementById('smtpFromUsername').value = config.emailServer.defaultSenderName || '';

        // Recipients
        const recipientsList = document.getElementById('recipientsList');
        recipientsList.innerHTML = '';
        if (config.emailServer.to && config.emailServer.to.length > 0) {
            config.emailServer.to.forEach(recipient => addRecipientField(recipient));
        } else {
            addRecipientField();
        }
    }
}

// Add URL Field
function addUrlField(value = '') {
    const urlsList = document.getElementById('urlsList');
    const div = document.createElement('div');
    div.className = 'url-item';
    div.innerHTML = `
        <input type="url" class="url-input" value="${value}" placeholder="https://example.com" required>
        <button type="button" class="btn btn-danger" onclick="this.parentElement.remove()">Remove</button>
    `;
    urlsList.appendChild(div);
}

// Add Recipient Field
function addRecipientField(value = '') {
    const recipientsList = document.getElementById('recipientsList');
    const div = document.createElement('div');
    div.className = 'recipient-item';
    div.innerHTML = `
        <input type="email" class="recipient-input" value="${value}" placeholder="admin@example.com" required>
        <button type="button" class="btn btn-danger" onclick="this.parentElement.remove()">Remove</button>
    `;
    recipientsList.appendChild(div);
}

// Submit Form
async function handleSubmit(e) {
    e.preventDefault();
    hideError();

    try {
        const formData = collectFormData();
        
        const response = await fetch('/api/config', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(formData)
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        showSuccess('Configuration saved successfully! Please restart the service for changes to take effect.');
        
        // Reload configuration
        setTimeout(() => loadConfig(), 2000);
    } catch (error) {
        showError(`Error saving configuration: ${error.message}`);
    }
}

// Collect Form Data
function collectFormData() {
    const urls = Array.from(document.querySelectorAll('.url-input'))
        .map(input => input.value.trim())
        .filter(url => url.length > 0);

    const recipients = Array.from(document.querySelectorAll('.recipient-input'))
        .map(input => input.value.trim())
        .filter(email => email.length > 0);

    return {
        system: {
            timeoutSeconds: Number.parseInt(document.getElementById('timeoutSeconds').value),
            daemonIntervalMinutes: Number.parseInt(document.getElementById('daemonIntervalMinutes').value),
            webUiPort: Number.parseInt(document.getElementById('webUiPort').value),
            runMode: document.getElementById('runMode').value
        },
        urls: urls,
        emailServer: {
            host: document.getElementById('smtpHost').value.trim(),
            port: Number.parseInt(document.getElementById('smtpPort').value),
            user: document.getElementById('smtpUsername').value.trim(),
            password: document.getElementById('smtpPassword').value.trim(),
            defaultEmailSenderAddress: document.getElementById('smtpFromEmail').value.trim(),
            defaultSenderName: document.getElementById('smtpFromUsername').value.trim(),
            to: recipients
        }
    };
}

// UI Helper Functions
function showLoading(show) {
    loadingEl.style.display = show ? 'block' : 'none';
}

function showError(message) {
    errorEl.textContent = message;
    errorEl.style.display = 'block';
}

function hideError() {
    errorEl.style.display = 'none';
}

function showSuccess(message) {
    const successDiv = document.createElement('div');
    successDiv.className = 'success-message';
    successDiv.textContent = message;
    
    configForm.parentElement.insertBefore(successDiv, configForm);
    
    setTimeout(() => successDiv.remove(), 5000);
}
