/**
 * Firebase Initialization
 * This script initializes Firebase and makes it available globally
 */

'use strict';

// Create a function to initialize Firebase
function initializeFirebase() {
  try {
    // Check if Firebase is already defined
    if (typeof firebase === 'undefined') {
      console.error("Firebase SDK not loaded. Make sure Firebase scripts are included before this script.");

      // Create a placeholder to prevent errors in other scripts
      window.firebase = {
        apps: [],
        firestore: function () {
          console.error("Firebase not properly initialized");
          return { collection: function () { return { doc: function () { return {}; } }; } };
        }
      };

      // Try to load Firebase scripts dynamically
      loadFirebaseScripts();
      return;
    }

    // Hardcoded Firebase configuration
    const firebaseConfig = {
      apiKey: "AIzaSyACWsakIQomRmJZShEOrXJ2z-XQOSr9Q5g",
      authDomain: "asp-sales.firebaseapp.com",
      projectId: "asp-sales",
      storageBucket: "asp-sales.firebasestorage.app",
      messagingSenderId: "277356792073",
      appId: "1:277356792073:web:5d676341f60b446cd96bd8"
    };

    // Initialize Firebase if not already initialized
    if (!firebase.apps.length) {
      firebase.initializeApp(firebaseConfig);
      console.log("Firebase initialized with hardcoded config");
    }

    // Make Firebase services available globally
    window.db = firebase.firestore();
    window.auth = firebase.auth();
    window.storage = firebase.storage();

    // Create a global promise that resolves when Firebase is ready
    window.firebaseReady = Promise.resolve({
      db: window.db,
      auth: window.auth,
      storage: window.storage
    });

    // Dispatch event to notify that Firebase is ready
    const event = new CustomEvent('firebase-ready', {
      detail: {
        db: window.db,
        auth: window.auth,
        storage: window.storage
      }
    });
    document.dispatchEvent(event);
    console.log('Firebase ready event dispatched');

  } catch (error) {
    console.error("Error initializing Firebase:", error);
    window.firebaseReady = Promise.reject(error);
  }
}

// Function to dynamically load Firebase scripts
function loadFirebaseScripts() {
  const scripts = [
    "https://www.gstatic.com/firebasejs/8.10.1/firebase-app.js",
    "https://www.gstatic.com/firebasejs/8.10.1/firebase-firestore.js",
    "https://www.gstatic.com/firebasejs/8.10.1/firebase-auth.js",
    "https://www.gstatic.com/firebasejs/8.10.1/firebase-storage.js"
  ];

  let scriptsLoaded = 0;

  scripts.forEach(src => {
    const script = document.createElement('script');
    script.src = src;
    script.async = true;

    script.onload = function () {
      scriptsLoaded++;
      if (scriptsLoaded === scripts.length) {
        console.log("Firebase scripts loaded dynamically");
        setTimeout(initializeFirebase, 500); // Give a small delay for scripts to initialize
      }
    };

    script.onerror = function () {
      console.error("Failed to load Firebase script:", src);
    };

    document.head.appendChild(script);
  });
}

// Initialize Firebase when the document is ready
document.addEventListener('DOMContentLoaded', initializeFirebase);

// Helper functions for user context
/*function getCurrentUserId() {
  return sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
}*/

function getCurrentUserId() {
  // First check if we have the user context from the server
  if (window.userContext && window.userContext.userId) {
    return window.userContext.userId;
  }

  // Fallback to session storage or cookies
  if (typeof sessionStorage !== 'undefined' && sessionStorage.getItem('UserId')) {
    return sessionStorage.getItem('UserId');
  }

  // Try to get from cookie
  const userIdMatch = document.cookie.match(/UserId=([^;]+)/);
  if (userIdMatch) {
    return userIdMatch[1];
  }

  return null;
}

/*function isUserAdmin() {
  return sessionStorage.getItem('IsAdmin') === '1';
}*/
// Helper function to check if user is admin
function isUserAdmin() {
  // First check if we have the user context from the server
  if (window.userContext) {
    return window.userContext.isAdmin;
  }

  // Fallback to session or cookies
  return document.cookie.includes('IsAdmin=1') ||
    (typeof sessionStorage !== 'undefined' && sessionStorage.getItem('IsAdmin') === '1');
}
function isUserAffiliate() {
  return sessionStorage.getItem('IsAffiliate') === '1';
}

function isUserCustomer() {
  return sessionStorage.getItem('IsCustomer') === '1';
}

// Format helpers that can be used across the application
function formatCurrency(amount) {
  if (amount === null || amount === undefined) return '$0.00';

  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(amount);
}

function formatDate(date) {
  if (!date) return '';

  // Handle Firestore Timestamp
  if (date && typeof date.toDate === 'function') {
    date = date.toDate();
  }

  // Handle string dates
  if (typeof date === 'string') {
    date = new Date(date);
  }

  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }).format(date);
}

function formatDateTime(date) {
  if (!date) return '';

  // Handle Firestore Timestamp
  if (date && typeof date.toDate === 'function') {
    date = date.toDate();
  }

  // Handle string dates
  if (typeof date === 'string') {
    date = new Date(date);
  }

  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date);
}

function getStatusBadgeClass(status) {
  if (!status) return 'bg-label-secondary';

  status = status.toLowerCase();

  switch (status) {
    case 'active':
    case 'completed':
    case 'paid':
    case 'delivered':
    case 'approved':
      return 'bg-label-success';

    case 'pending':
    case 'processing':
    case 'in progress':
      return 'bg-label-warning';

    case 'cancelled':
    case 'rejected':
    case 'failed':
      return 'bg-label-danger';

    case 'shipped':
    case 'on hold':
      return 'bg-label-info';

    case 'inactive':
    case 'disabled':
      return 'bg-label-secondary';

    default:
      return 'bg-label-primary';
  }
}
