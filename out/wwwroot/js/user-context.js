// User Context Manager
document.addEventListener('DOMContentLoaded', function () {
  // Parse user data from embedded script
  initializeUserContext();

  // Wait for Firebase to be ready
  document.addEventListener('firebase-ready', function () {
    validateUserContext();
  });
});

// Initialize user context from server-provided data
function initializeUserContext() {
  try {
    const userContextElement = document.getElementById('user-context');
    if (userContextElement) {
      const userContext = JSON.parse(userContextElement.textContent);

      // Store in window object for immediate access
      window.userContext = userContext;

      // Also store in sessionStorage as backup
      sessionStorage.setItem('userId', userContext.userId);
      sessionStorage.setItem('isAdmin', userContext.isAdmin ? '1' : '0');
      sessionStorage.setItem('isAffiliate', userContext.isAffiliate ? '1' : '0');
      sessionStorage.setItem('isCustomer', userContext.isCustomer ? '1' : '0');

      console.log('User context initialized from server data');
    } else {
      // Try to restore from sessionStorage
      window.userContext = {
        userId: sessionStorage.getItem('userId'),
        isAdmin: sessionStorage.getItem('isAdmin') === '1',
        isAffiliate: sessionStorage.getItem('isAffiliate') === '1',
        isCustomer: sessionStorage.getItem('isCustomer') === '1'
      };

      console.log('User context restored from session storage');
    }
  } catch (error) {
    console.error('Error initializing user context:', error);
    window.userContext = { userId: null, isAdmin: false, isAffiliate: false, isCustomer: false };
  }

  // Update UI based on user context
  updateUIForUserContext();
}

// Validate user context against Firestore data
function validateUserContext() {
  const userId = window.userContext?.userId;

  if (!userId) return;

  // Verify user role with Firestore
  window.db.collection('users').doc(userId).get()
    .then(doc => {
      if (doc.exists) {
        const userData = doc.data();
        const role = userData.Role || userData.role;

        // Update user context if role doesn't match
        const isAdmin = role === 1 || role === '1';
        const isAffiliate = role === 2 || role === '2';
        const isCustomer = role === 3 || role === '3';

        if (isAdmin !== window.userContext.isAdmin ||
          isAffiliate !== window.userContext.isAffiliate ||
          isCustomer !== window.userContext.isCustomer) {

          console.log('User role updated from Firestore');

          window.userContext.isAdmin = isAdmin;
          window.userContext.isAffiliate = isAffiliate;
          window.userContext.isCustomer = isCustomer;

          sessionStorage.setItem('isAdmin', isAdmin ? '1' : '0');
          sessionStorage.setItem('isAffiliate', isAffiliate ? '1' : '0');
          sessionStorage.setItem('isCustomer', isCustomer ? '1' : '0');

          // Update UI for new role
          updateUIForUserContext();
        }
      }
    })
    .catch(error => {
      console.error('Error validating user context:', error);
    });
}

// Update UI based on user context
function updateUIForUserContext() {
  const { userId, isAdmin, isAffiliate, isCustomer } = window.userContext || {};

  // Hide all role-specific elements first
  document.querySelectorAll('[data-role]').forEach(el => {
    el.style.display = 'none';
  });

  // Show elements based on role
  if (userId) {
    // User is logged in
    document.querySelectorAll('[data-role="user"]').forEach(el => {
      el.style.display = '';
    });

    if (isAdmin) {
      document.querySelectorAll('[data-role="admin"]').forEach(el => {
        el.style.display = '';
      });
    }

    if (isAffiliate) {
      document.querySelectorAll('[data-role="affiliate"]').forEach(el => {
        el.style.display = '';
      });
    }

    if (isCustomer) {
      document.querySelectorAll('[data-role="customer"]').forEach(el => {
        el.style.display = '';
      });
    }
  } else {
    // User is not logged in
    document.querySelectorAll('[data-role="guest"]').forEach(el => {
      el.style.display = '';
    });
  }
}

// Get current user ID
function getCurrentUserId() {
  return window.userContext?.userId || null;
}

// Check if user is admin
function isUserAdmin() {
  return window.userContext?.isAdmin || false;
}

// Check if user is affiliate
function isUserAffiliate() {
  return window.userContext?.isAffiliate || false;
}

// Check if user is customer
function isUserCustomer() {
  return window.userContext?.isCustomer || false;
}

// Check if user is logged in
function isUserLoggedIn() {
  return !!window.userContext?.userId;
}

// Refresh user context
function refreshUserContext() {
  validateUserContext();
}
