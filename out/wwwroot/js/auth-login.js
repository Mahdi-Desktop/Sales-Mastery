/**
 * Authentication Login Handler
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  const loginForm = document.getElementById('formAuthentication');

  if (loginForm) {
    // Add client-side validation
    loginForm.addEventListener('submit', function (e) {
      console.log('Login form submit triggered');

      const emailInput = document.getElementById('Email');
      const passwordInput = document.getElementById('Password');

      if (!emailInput || !passwordInput) {
        console.error('Email or Password input elements not found');
        return true; // Allow form to submit if elements not found
      }

      const email = emailInput.value.trim();
      const password = passwordInput.value;

      if (!email || !password) {
        e.preventDefault();
        showError('Please enter both email and password');
        return false;
      }

      // Show loading indicator
      const submitBtn = loginForm.querySelector('button[type="submit"]');
      if (submitBtn) {
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Logging in...';
        submitBtn.disabled = true;
      }

      console.log('Form validation passed, submitting...');
      // Form will submit normally to the server
      return true;
    });
  } else {
    console.error('Login form element not found');
  }

  function showError(message) {
    // Check if error container exists, if not create it
    let errorContainer = document.querySelector('.validation-summary-errors');

    if (!errorContainer) {
      errorContainer = document.createElement('div');
      errorContainer.className = 'validation-summary-errors text-danger';
      errorContainer.innerHTML = '<ul><li>' + message + '</li></ul>';
      loginForm.prepend(errorContainer);
    } else {
      // Update existing error container
      const errorList = errorContainer.querySelector('ul');
      errorList.innerHTML = '<li>' + message + '</li>';
    }
  }
});
