/**
 * Session Manager
 * Handles session timeout and redirects
 */

'use strict';

(function () {
  // Session timeout in milliseconds (30 minutes)
  const SESSION_TIMEOUT = 30 * 60 * 1000;
  let sessionTimer;

  function resetSessionTimer() {
    clearTimeout(sessionTimer);
    sessionTimer = setTimeout(handleSessionTimeout, SESSION_TIMEOUT);
  }

  function handleSessionTimeout() {
    // Redirect to login page
    window.location.href = '/Auth/LoginBasic';
  }

  // Reset timer on user activity
  function setupActivityListeners() {
    const events = ['mousedown', 'keypress', 'scroll', 'touchstart'];
    events.forEach(event => {
      document.addEventListener(event, resetSessionTimer, false);
    });
  }

  // Check if user is logged in
  function isLoggedIn() {
    return sessionStorage.getItem('UserId') ||
      localStorage.getItem('UserId') ||
      document.cookie.includes('UserId=');
  }

  // Initialize
  document.addEventListener('DOMContentLoaded', function () {
    if (isLoggedIn()) {
      resetSessionTimer();
      setupActivityListeners();
    }
  });

  // Expose session check method globally
  window.checkSession = function () {
    if (!isLoggedIn()) {
      window.location.href = '/Auth/LoginBasic';
      return false;
    }
    return true;
  };
})();
