// Initialize user data from embedded script or storage
document.addEventListener('DOMContentLoaded', function () {
  // Try to parse user data from embedded script
  window.userData = null;
  try {
    const userDataScript = document.getElementById('user-data');
    if (userDataScript) {
      window.userData = JSON.parse(userDataScript.textContent);

      // Store user data in session storage for other pages
      if (window.userData && window.userData.userId) {
        sessionStorage.setItem('UserId', window.userData.userId);

        if (window.userData.isAdmin) {
          sessionStorage.setItem('IsAdmin', '1');
        }

        if (window.userData.isAffiliate) {
          sessionStorage.setItem('IsAffiliate', '1');
        }

        if (window.userData.isCustomer) {
          sessionStorage.setItem('IsCustomer', '1');
        }
      }
    } else {
      // If no embedded script, try to get from session storage
      const userId = sessionStorage.getItem('UserId');
      if (userId) {
        window.userData = {
          userId: userId,
          isAdmin: sessionStorage.getItem('IsAdmin') === '1',
          isAffiliate: sessionStorage.getItem('IsAffiliate') === '1',
          isCustomer: sessionStorage.getItem('IsCustomer') === '1'
        };
      }
    }
  } catch (e) {
    console.error('Error parsing user data:', e);
  }

  // Dispatch event that user data is ready
  document.dispatchEvent(new CustomEvent('user-data-ready', {
    detail: { userData: window.userData }
  }));

  // Update UI based on user login status
  updateUIForUserStatus();
});

// Get user ID from any available source
function getStoredUserId() {
  // Try window.userData first
  if (window.userData && window.userData.userId) {
    return window.userData.userId;
  }

  // Then try session/local storage
  return sessionStorage.getItem('UserId') || localStorage.getItem('UserId');
}

// Check if user is logged in
function isUserLoggedIn() {
  return !!getStoredUserId();
}

// Check if user is admin
function isUserAdmin() {
  if (window.userData) {
    return window.userData.isAdmin;
  }
  return sessionStorage.getItem('IsAdmin') === '1';
}

// Check if user is affiliate
function isUserAffiliate() {
  if (window.userData) {
    return window.userData.isAffiliate;
  }
  return sessionStorage.getItem('IsAffiliate') === '1';
}

// Update UI elements based on user login status
function updateUIForUserStatus() {
  const loggedIn = isUserLoggedIn();

  // Update login/logout buttons
  document.querySelectorAll('.login-btn').forEach(el => {
    el.style.display = loggedIn ? 'none' : '';
  });

  document.querySelectorAll('.logout-btn').forEach(el => {
    el.style.display = loggedIn ? '' : 'none';
  });

  // Update user-only elements
  document.querySelectorAll('.user-only').forEach(el => {
    el.style.display = loggedIn ? '' : 'none';
  });

  // Update admin-only elements
  document.querySelectorAll('.admin-only').forEach(el => {
    el.style.display = isUserAdmin() ? '' : 'none';
  });

  // Update affiliate-only elements
  document.querySelectorAll('.affiliate-only').forEach(el => {
    el.style.display = isUserAffiliate() ? '' : 'none';
  });
}
