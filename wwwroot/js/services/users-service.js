/**
 * Users Service
 * Handles all user-related operations with Firebase Firestore
 */

const usersService = {
  /**
   * Get user by ID
   * @param {string} userId - The user ID
   * @returns {Promise<object>} - User data object
   */
  getUserById: async function (userId) {
    try {
      // Wait for Firebase to be ready if needed
      if (!firebaseInitialized) {
        await new Promise(resolve => {
          document.addEventListener('firebase-ready', resolve, { once: true });
        });
      }

      const db = firebase.firestore();
      const userDoc = await db.collection('users').doc(userId).get();

      if (!userDoc.exists) {
        console.error('User not found:', userId);
        return null;
      }

      const userData = userDoc.data();
      userData.userId = userDoc.id;

      return userData;
    } catch (error) {
      console.error('Error getting user:', error);
      return null;
    }
  },

  /**
   * Update user profile
   * @param {string} userId - The user ID
   * @param {object} userData - Object containing user data to update
   * @returns {Promise<boolean>} - Success status
   */
  updateUser: async function (userId, userData) {
    try {
      // Wait for Firebase to be ready if needed
      if (!firebaseInitialized) {
        await new Promise(resolve => {
          document.addEventListener('firebase-ready', resolve, { once: true });
        });
      }

      const db = firebase.firestore();

      // Add timestamp
      userData.UpdatedAt = firebase.firestore.FieldValue.serverTimestamp();

      // Update user document
      await db.collection('users').doc(userId).update(userData);
      return true;
    } catch (error) {
      console.error('Error updating user:', error);
      return false;
    }
  },

  /**
   * Change user password
   * @param {string} userId - The user ID
   * @param {string} currentPassword - Current password
   * @param {string} newPassword - New password
   * @returns {Promise<object>} - Result object with success status and message
   */
  changePassword: async function (userId, currentPassword, newPassword) {
    try {
      // Wait for Firebase to be ready if needed
      if (!firebaseInitialized) {
        await new Promise(resolve => {
          document.addEventListener('firebase-ready', resolve, { once: true });
        });
      }

      // Use the server API for password changes to properly handle hashing
      const response = await fetch('/Users/ChangePassword', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: new URLSearchParams({
          userId: userId,
          currentPassword: currentPassword,
          newPassword: newPassword,
          confirmPassword: newPassword
        })
      });

      // Parse the JSON response
      const result = await response.json();
      return result;
    } catch (error) {
      console.error('Error changing password:', error);
      return { success: false, message: 'An error occurred: ' + error.message };
    }
  },

  /**
   * Get user address
   * @param {string} userId - The user ID
   * @returns {Promise<object>} - Address data
   */
  getUserAddress: async function (userId) {
    try {
      // Wait for Firebase to be ready if needed
      if (!firebaseInitialized) {
        await new Promise(resolve => {
          document.addEventListener('firebase-ready', resolve, { once: true });
        });
      }

      const db = firebase.firestore();
      const addressSnapshot = await db.collection('addresses').where('UserId', '==', userId).limit(1).get();

      if (addressSnapshot.empty) {
        return null;
      }

      const addressDoc = addressSnapshot.docs[0];
      const addressData = addressDoc.data();
      addressData.addressId = addressDoc.id;

      return addressData;
    } catch (error) {
      console.error('Error getting address:', error);
      return null;
    }
  },

  /**
   * Save user address (create or update)
   * @param {string} userId - The user ID
   * @param {object} addressData - Address data object
   * @returns {Promise<boolean>} - Success status
   */
  saveUserAddress: async function (userId, addressData) {
    try {
      // Wait for Firebase to be ready if needed
      if (!firebaseInitialized) {
        await new Promise(resolve => {
          document.addEventListener('firebase-ready', resolve, { once: true });
        });
      }

      const db = firebase.firestore();

      // Add user ID and timestamps
      addressData.UserId = userId;
      addressData.UpdatedAt = firebase.firestore.FieldValue.serverTimestamp();

      // Check if user already has an address
      const addressSnapshot = await db.collection('addresses').where('UserId', '==', userId).limit(1).get();

      if (addressSnapshot.empty) {
        // Create new address
        addressData.CreatedAt = firebase.firestore.FieldValue.serverTimestamp();
        await db.collection('addresses').add(addressData);
      } else {
        // Update existing address
        const addressId = addressSnapshot.docs[0].id;
        await db.collection('addresses').doc(addressId).update(addressData);
      }

      return true;
    } catch (error) {
      console.error('Error saving address:', error);
      return false;
    }
  },

  /**
   * Helper function to get current user ID from various sources
   */
  getCurrentUserId: function () {
    // From window.userContext
    if (window.userContext && window.userContext.userId) {
      return window.userContext.userId;
    }

    // From session storage
    const sessionUserId = sessionStorage.getItem('UserId');
    if (sessionUserId) {
      return sessionUserId;
    }

    // From local storage
    const localUserId = localStorage.getItem('UserId');
    if (localUserId) {
      return localUserId;
    }

    return null;
  }
};
