class Chatbot {
    constructor() {
        this.isOpen = false;
        this.messages = [];
        this.init();
    }

    init() {
        this.createChatbotUI();
        this.loadMessages();
        this.setupEventListeners();
        this.addWelcomeMessage();
    }

    createChatbotUI() {
        // Create chatbot container
        const chatbotHTML = `
            <div id="chatbot-container" class="chatbot-container">
                <div class="chatbot-header" id="chatbot-header">
                    <div class="chatbot-header-content">
                        <i class="bi bi-chat-dots-fill"></i>
                        <span>Safe Voice Assistant</span>
                    </div>
                    <div class="chatbot-header-actions">
                        <button id="chatbot-minimize" class="chatbot-btn">
                            <i class="bi bi-dash"></i>
                        </button>
                        <button id="chatbot-close" class="chatbot-btn">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                </div>
                <div id="chatbot-messages" class="chatbot-messages">
                    <div class="chatbot-loading" id="chatbot-loading" style="display: none;">
                        <div class="dot-floating"></div>
                    </div>
                </div>
                <div class="chatbot-input-container">
                    <input type="text" id="chatbot-input" placeholder="Type your message..." class="chatbot-input">
                    <button id="chatbot-send" class="chatbot-send-btn">
                        <i class="bi bi-send"></i>
                    </button>
                </div>
            </div>
            <button id="chatbot-toggle" class="chatbot-toggle">
                <i class="bi bi-chat-dots-fill"></i>
                <span class="chatbot-badge">?</span>
            </button>
        `;

        document.body.insertAdjacentHTML('beforeend', chatbotHTML);
    }

    setupEventListeners() {
        const toggleBtn = document.getElementById('chatbot-toggle');
        const closeBtn = document.getElementById('chatbot-close');
        const minimizeBtn = document.getElementById('chatbot-minimize');
        const sendBtn = document.getElementById('chatbot-send');
        const input = document.getElementById('chatbot-input');

        toggleBtn.addEventListener('click', () => this.toggleChat());
        closeBtn.addEventListener('click', () => this.closeChat());
        minimizeBtn.addEventListener('click', () => this.minimizeChat());
        sendBtn.addEventListener('click', () => this.sendMessage());
        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') this.sendMessage();
        });
    }

    toggleChat() {
        const container = document.getElementById('chatbot-container');
        const toggleBtn = document.getElementById('chatbot-toggle');

        if (this.isOpen) {
            container.classList.remove('open');
            toggleBtn.style.display = 'flex';
            this.isOpen = false;
        } else {
            container.classList.add('open');
            toggleBtn.style.display = 'none';
            this.isOpen = true;
            document.getElementById('chatbot-input').focus();
        }
    }

    closeChat() {
        const container = document.getElementById('chatbot-container');
        const toggleBtn = document.getElementById('chatbot-toggle');

        container.classList.remove('open');
        toggleBtn.style.display = 'flex';
        this.isOpen = false;
    }

    minimizeChat() {
        const container = document.getElementById('chatbot-container');
        container.classList.toggle('minimized');
    }

    addWelcomeMessage() {
        setTimeout(() => {
            this.addMessage("Hello! 👋 I'm Safe Voice Assistant. How can I help you today? You can ask me about:\n\n• How to submit a report\n• Anonymous reporting\n• Support services\n• Tracking your report\n• Safety tips\n• Emergency contacts", 'bot');
        }, 500);
    }

    addMessage(text, sender) {
        const messagesContainer = document.getElementById('chatbot-messages');
        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbot-message ${sender}`;

        const time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        messageDiv.innerHTML = `
            <div class="chatbot-message-content">
                ${this.formatMessage(text)}
                <div class="chatbot-message-time">${time}</div>
            </div>
        `;

        messagesContainer.appendChild(messageDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;

        // Save to localStorage
        this.saveMessage({ text, sender, time });
    }

    formatMessage(text) {
        // Replace newlines with <br>
        let formatted = text.replace(/\n/g, '<br>');

        // Format bold text
        formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

        // Format bullet points
        formatted = formatted.replace(/•/g, '•');

        return formatted;
    }

    async sendMessage() {
        const input = document.getElementById('chatbot-input');
        const message = input.value.trim();

        if (!message) return;

        // Add user message to chat
        this.addMessage(message, 'user');
        input.value = '';

        // Show loading
        const loading = document.getElementById('chatbot-loading');
        loading.style.display = 'flex';

        try {
            // Send to server
            const response = await fetch('/Chatbot/SendMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({ message: message })
            });

            const data = await response.json();

            // Hide loading
            loading.style.display = 'none';

            // Add bot response
            if (data.success) {
                this.addMessage(data.response, 'bot');
            } else {
                this.addMessage("I'm having trouble connecting. Please try again or contact support.", 'bot');
            }
        } catch (error) {
            console.error('Error:', error);
            loading.style.display = 'none';
            this.addMessage("Sorry, I'm having technical difficulties. Please try again later.", 'bot');
        }
    }

    saveMessage(message) {
        let messages = JSON.parse(localStorage.getItem('chatbot_messages') || '[]');
        messages.push(message);
        // Keep last 50 messages
        if (messages.length > 50) messages = messages.slice(-50);
        localStorage.setItem('chatbot_messages', JSON.stringify(messages));
    }

    loadMessages() {
        const messages = JSON.parse(localStorage.getItem('chatbot_messages') || '[]');
        messages.forEach(msg => {
            this.addMessage(msg.text, msg.sender);
        });
    }
}

// Initialize chatbot when page loads
document.addEventListener('DOMContentLoaded', () => {
    window.chatbot = new Chatbot();
});