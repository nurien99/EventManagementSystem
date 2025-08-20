// ================================
// DROPDOWN FUNCTIONALITY
// ================================

// Toggle specific dropdown by ID
window.toggleDropdown = function(elementId) {
    try {
        console.log('Attempting to toggle dropdown:', elementId);
        const element = document.getElementById(elementId);
        if (!element) {
            console.error(`Element with ID ${elementId} not found`);
            return false;
        }
        
        // Check if Bootstrap is available
        if (typeof bootstrap === 'undefined') {
            console.error('Bootstrap is not available');
            return false;
        }
        
        const dropdown = bootstrap.Dropdown.getOrCreateInstance(element);
        dropdown.toggle();
        console.log(`Successfully toggled dropdown: ${elementId}`);
        return true;
    } catch (error) {
        console.error(`Error toggling dropdown ${elementId}:`, error);
        return false;
    }
};

// Force initialize a specific dropdown
window.initializeDropdown = function(elementId) {
    try {
        console.log('Initializing dropdown:', elementId);
        const element = document.getElementById(elementId);
        if (!element) {
            console.error(`Element with ID ${elementId} not found`);
            return false;
        }
        
        // Check if Bootstrap is available
        if (typeof bootstrap === 'undefined') {
            console.error('Bootstrap is not available');
            return false;
        }
        
        // Dispose existing instance if any
        const existingDropdown = bootstrap.Dropdown.getInstance(element);
        if (existingDropdown) {
            existingDropdown.dispose();
        }
        
        // Create new dropdown instance
        const dropdown = new bootstrap.Dropdown(element, {
            autoClose: true,
            boundary: 'viewport',
            display: 'dynamic'
        });
        
        console.log(`Initialized dropdown for ${elementId}`);
        return true;
    } catch (error) {
        console.error(`Error initializing dropdown ${elementId}:`, error);
        return false;
    }
};

// Initialize Bootstrap dropdowns
window.initializeBootstrapDropdowns = function() {
    try {
        console.log('Starting Bootstrap dropdowns initialization...');
        
        // Check if Bootstrap is available
        if (typeof bootstrap === 'undefined') {
            console.error('Bootstrap is not available');
            return;
        }
        
        // Wait for DOM to be ready
        setTimeout(() => {
            try {
                // Dispose existing dropdowns first
                const existingDropdowns = document.querySelectorAll('.dropdown-toggle');
                existingDropdowns.forEach(element => {
                    const existingDropdown = bootstrap.Dropdown.getInstance(element);
                    if (existingDropdown) {
                        existingDropdown.dispose();
                    }
                });

                // Initialize all dropdown toggles
                const dropdownElementList = document.querySelectorAll('.dropdown-toggle');
                let initializedCount = 0;
                
                dropdownElementList.forEach(dropdownToggleEl => {
                    try {
                        // Ensure proper data attributes
                        dropdownToggleEl.setAttribute('data-bs-toggle', 'dropdown');
                        dropdownToggleEl.setAttribute('aria-expanded', 'false');
                        
                        const dropdown = new bootstrap.Dropdown(dropdownToggleEl, {
                            autoClose: true,
                            boundary: 'viewport',
                            display: 'dynamic'
                        });
                        
                        initializedCount++;
                    } catch (error) {
                        console.error('Error initializing individual dropdown:', error, dropdownToggleEl);
                    }
                });
                
                console.log(`Successfully initialized ${initializedCount} dropdowns out of ${dropdownElementList.length} found`);
            } catch (error) {
                console.error('Error in dropdown initialization timeout:', error);
            }
        }, 200);
    } catch (error) {
        console.error('Error in initializeBootstrapDropdowns:', error);
    }
};

// Initialize specific navigation dropdowns
window.initializeNavigationDropdowns = function() {
    try {
        console.log('Initializing navigation dropdowns...');
        
        const dropdownIds = ['userMenuButton', 'adminDropdown', 'myEventsDropdown'];
        
        dropdownIds.forEach(id => {
            const element = document.getElementById(id);
            if (element) {
                window.initializeDropdown(id);
            } else {
                console.log(`Navigation dropdown element not found: ${id}`);
            }
        });
        
        // Also run general dropdown initialization
        window.initializeBootstrapDropdowns();
        
        console.log('Navigation dropdowns initialization completed');
    } catch (error) {
        console.error('Error in initializeNavigationDropdowns:', error);
    }
};

// ================================
// INITIALIZATION EVENTS
// ================================

// Initialize dropdowns when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM Content Loaded - initializing dropdowns');
    window.initializeNavigationDropdowns();
});

// Reinitialize after Blazor renders
window.addEventListener('load', function() {
    console.log('Window loaded - initializing dropdowns');
    setTimeout(() => {
        window.initializeNavigationDropdowns();
    }, 1000);
});

// Check for Blazor events
if (window.Blazor) {
    window.Blazor.addEventListener('enhancedload', function() {
        console.log('Blazor enhanced load - initializing dropdowns');
        window.initializeNavigationDropdowns();
    });
}

// ================================
// MOBILE NAVIGATION FUNCTIONS
// ================================

// Mobile Navigation Helper Functions
window.closeMobileMenuOnResize = (dotNetHelper) => {
    const handleResize = () => {
        if (window.innerWidth > 768) {
            dotNetHelper.invokeMethodAsync('CloseMobileMenuOnResize');
        }
    };
    
    window.addEventListener('resize', handleResize);
    
    // Return cleanup function
    return () => {
        window.removeEventListener('resize', handleResize);
    };
};

// Prevent body scroll when mobile menu is open
window.preventBodyScroll = (prevent) => {
    if (prevent) {
        document.body.style.overflow = 'hidden';
        document.body.style.position = 'fixed';
        document.body.style.width = '100%';
    } else {
        document.body.style.overflow = '';
        document.body.style.position = '';
        document.body.style.width = '';
    }
};

// Close mobile menu when clicking outside
window.setupMobileMenuClickOutside = (dotNetHelper) => {
    const handleClickOutside = (event) => {
        const sidebar = document.querySelector('.sidebar');
        const toggleButton = document.querySelector('.mobile-nav-toggle');
        
        if (sidebar && toggleButton && 
            !sidebar.contains(event.target) && 
            !toggleButton.contains(event.target) &&
            sidebar.classList.contains('show')) {
            dotNetHelper.invokeMethodAsync('CloseMobileMenuOnResize');
        }
    };
    
    document.addEventListener('click', handleClickOutside);
    
    // Return cleanup function
    return () => {
        document.removeEventListener('click', handleClickOutside);
    };
};

// Smooth scroll to top when navigating
window.scrollToTop = () => {
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
};

// Initialize mobile navigation
window.initializeMobileNav = (dotNetHelper) => {
    // Setup resize listener
    const resizeCleanup = window.closeMobileMenuOnResize(dotNetHelper);
    
    // Setup click outside listener
    const clickCleanup = window.setupMobileMenuClickOutside(dotNetHelper);
    
    // Return cleanup function for both listeners
    return () => {
        resizeCleanup();
        clickCleanup();
    };
};

// ================================
// FILE DOWNLOAD FUNCTION
// ================================

// Download file with authentication
window.downloadFileWithAuth = async (url, filename) => {
    try {
        // Get the authentication token from localStorage
        const token = localStorage.getItem('authToken');
        
        if (!token) {
            alert('❌ Authentication required to download ticket');
            return;
        }
        
        // Fetch the file with authentication headers
        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/pdf'
            }
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        // Get the blob data
        const blob = await response.blob();
        
        // Create a download link and trigger it
        const downloadUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(downloadUrl);
        
        console.log(`✅ Downloaded: ${filename}`);
    } catch (error) {
        console.error('❌ Error downloading file:', error);
        alert(`❌ Error downloading ticket: ${error.message}`);
    }
};

// ================================
// FILE DOWNLOAD FROM BASE64
// ================================

// Download file from base64 content
window.downloadFileFromBase64 = (base64Content, filename, contentType) => {
    try {
        // Convert base64 to blob
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: contentType });
        
        // Create download link and trigger it
        const downloadUrl = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = downloadUrl;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(downloadUrl);
        
        console.log(`✅ Downloaded: ${filename}`);
    } catch (error) {
        console.error('❌ Error downloading file from base64:', error);
        alert(`❌ Error downloading file: ${error.message}`);
    }
};

// ================================
// QR CODE SCANNER FUNCTIONALITY
// ================================

let qrScanner = null;
let blazorReference = null;
let currentEventId = null;

// Initialize QR Scanner
window.initializeQRScanner = function(dotNetRef, eventId) {
    try {
        blazorReference = dotNetRef;
        currentEventId = eventId;
        console.log(`✅ QR Scanner initialized for event ${eventId}`);
    } catch (error) {
        console.error('❌ Error initializing QR Scanner:', error);
    }
};

// Start QR Scanner
window.startQRScanner = function() {
    try {
        const video = document.createElement('video');
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d');
        
        const cameraPreview = document.getElementById('cameraPreview');
        const startBtn = document.getElementById('startScanBtn');
        const stopBtn = document.getElementById('stopScanBtn');
        
        if (!cameraPreview) {
            console.error('❌ Camera preview element not found');
            return;
        }
        
        // Clear previous content and add video
        cameraPreview.innerHTML = '';
        cameraPreview.appendChild(video);
        
        // Start camera
        navigator.mediaDevices.getUserMedia({ 
            video: { 
                facingMode: 'environment',  // Use back camera on mobile
                width: { ideal: 640 },
                height: { ideal: 480 }
            } 
        })
        .then(stream => {
            video.srcObject = stream;
            video.play();
            
            // Update UI
            if (startBtn) startBtn.style.display = 'none';
            if (stopBtn) stopBtn.style.display = 'inline-block';
            
            // Start scanning
            scanForQRCode(video, canvas, context);
            console.log('✅ QR Scanner started successfully');
        })
        .catch(error => {
            console.error('❌ Error accessing camera:', error);
            alert('❌ Error accessing camera. Please ensure camera permissions are granted.');
        });
        
    } catch (error) {
        console.error('❌ Error starting QR Scanner:', error);
        alert('❌ Error starting scanner');
    }
};

// Stop QR Scanner
window.stopQRScanner = function() {
    try {
        const cameraPreview = document.getElementById('cameraPreview');
        const startBtn = document.getElementById('startScanBtn');
        const stopBtn = document.getElementById('stopScanBtn');
        
        // Stop all video streams
        const video = cameraPreview?.querySelector('video');
        if (video && video.srcObject) {
            const tracks = video.srcObject.getTracks();
            tracks.forEach(track => track.stop());
        }
        
        // Clear scanner interval
        if (qrScanner) {
            clearInterval(qrScanner);
            qrScanner = null;
        }
        
        // Reset UI
        if (cameraPreview) {
            cameraPreview.innerHTML = `
                <div class="scanner-overlay">
                    <div class="scan-frame"></div>
                    <p class="scan-instruction">Position QR code within the frame</p>
                </div>
            `;
        }
        
        if (startBtn) startBtn.style.display = 'inline-block';
        if (stopBtn) stopBtn.style.display = 'none';
        
        console.log('✅ QR Scanner stopped successfully');
    } catch (error) {
        console.error('❌ Error stopping QR Scanner:', error);
    }
};

// Scan for QR Code
function scanForQRCode(video, canvas, context) {
    qrScanner = setInterval(() => {
        if (video.readyState === video.HAVE_ENOUGH_DATA) {
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            context.drawImage(video, 0, 0);
            
            const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
            
            // Try to decode QR code (using a simple approach for demo)
            // In production, you'd use a library like jsQR
            const qrCode = detectQRCode(imageData);
            if (qrCode) {
                stopQRScanner();
                processScannedQR(qrCode);
            }
        }
    }, 200); // Scan every 200ms
}

// Simple QR detection (placeholder - in production use jsQR library)
function detectQRCode(imageData) {
    // This is a simplified placeholder
    // In production, use jsQR library: https://github.com/cozmo/jsQR
    // For now, return null to simulate no QR detected
    return null;
}

// Process scanned QR code
function processScannedQR(qrCodeData) {
    try {
        console.log(`📱 QR Code scanned: ${qrCodeData}`);
        
        if (blazorReference) {
            blazorReference.invokeMethodAsync('OnQRCodeScanned', qrCodeData);
            playSuccessSound();
        } else {
            console.error('❌ Blazor reference not available');
        }
    } catch (error) {
        console.error('❌ Error processing scanned QR:', error);
        playErrorSound();
    }
}

// Audio feedback functions
window.playSuccessSound = function() {
    try {
        // Create a success sound (short beep)
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();
        
        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);
        
        oscillator.frequency.value = 800; // Success frequency
        oscillator.type = 'sine';
        
        gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.3);
        
        oscillator.start(audioContext.currentTime);
        oscillator.stop(audioContext.currentTime + 0.3);
    } catch (error) {
        console.log('Audio not supported or blocked');
    }
};

window.playErrorSound = function() {
    try {
        // Create an error sound (lower frequency beep)
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();
        
        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);
        
        oscillator.frequency.value = 300; // Error frequency
        oscillator.type = 'square';
        
        gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.5);
        
        oscillator.start(audioContext.currentTime);
        oscillator.stop(audioContext.currentTime + 0.5);
    } catch (error) {
        console.log('Audio not supported or blocked');
    }
};

// Clean up when page unloads
window.addEventListener('beforeunload', function() {
    if (qrScanner) {
        stopQRScanner();
    }
});

// ================================
// EVENT SELECTOR FUNCTIONALITY
// ================================

// Custom navigation function for consistent URL navigation
window.navigateToUrl = function(url) {
    try {
        console.log('🔵 JS navigateToUrl called with:', url);
        window.location.href = url;
    } catch (error) {
        console.error('🔴 Navigation error:', error);
    }
};

// Show event selector modal for check-in or assistant management
window.showEventSelector = function(type) {
    try {
        // For now, redirect to My Events page with a hash parameter
        // In a full implementation, this would show a modal with event selection
        const currentUrl = window.location.href;
        const baseUrl = currentUrl.split('#')[0].split('?')[0];
        
        if (type === 'checkin') {
            window.location.href = `/my-events#checkin-mode`;
        } else if (type === 'assistants') {
            window.location.href = `/my-events#assistant-mode`;
        }
        
        console.log(`✅ Navigating to ${type} mode`);
    } catch (error) {
        console.error('❌ Error showing event selector:', error);
        alert('Please select an event from My Events page first.');
    }
};

// ================================
// ALERT FUNCTION
// ================================

// Show alert messages with different types
window.showAlert = (type, message) => {
    try {
        let icon = '';
        let title = '';
        
        switch(type.toLowerCase()) {
            case 'success':
                icon = '✅';
                title = 'Success';
                break;
            case 'error':
            case 'danger':
                icon = '❌';
                title = 'Error';
                break;
            case 'warning':
                icon = '⚠️';
                title = 'Warning';
                break;
            case 'info':
                icon = 'ℹ️';
                title = 'Info';
                break;
            default:
                icon = 'ℹ️';
                title = 'Notice';
        }
        
        // Use browser alert for now (can be enhanced later with custom modal)
        alert(`${icon} ${title}\n\n${message}`);
        
        // Log to console for debugging
        console.log(`${type.toUpperCase()}: ${message}`);
        
    } catch (error) {
        console.error('Error in showAlert:', error);
        // Fallback to simple alert
        alert(message);
    }
};

console.log('Site.js loaded successfully');