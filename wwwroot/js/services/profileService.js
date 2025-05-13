const profileService = {
  getAffiliateProfile: async function (userId) {
    try {
      // Get the affiliate record
      const affiliateSnapshot = await firebaseService.db
        .collection('affiliates')
        .where('UserId', '==', userId)
        .limit(1)
        .get();

      if (affiliateSnapshot.empty) {
        return null;
      }

      const affiliateDoc = affiliateSnapshot.docs[0];
      const affiliate = {
        affiliateId: affiliateDoc.id,
        ...affiliateDoc.data()
      };

      // Get the user record
      const userDoc = await firebaseService.db.collection('users').doc(userId).get();

      if (!userDoc.exists) {
        return null;
      }

      const user = {
        userId: userDoc.id,
        ...userDoc.data()
      };

      return { affiliate, user };
    } catch (error) {
      console.error('Error getting affiliate profile:', error);
      return null;
    }
  },

  updateProfile: async function (userId, data) {
    try {
      // Update user data
      if (data.firstName || data.lastName || data.phoneNumber) {
        await firebaseService.db
          .collection('users')
          .doc(userId)
          .update({
            FirstName: data.firstName,
            LastName: data.lastName,
            PhoneNumber: data.phoneNumber,
            UpdatedAt: firebase.firestore.Timestamp.fromDate(new Date())
          });
      }

      return true;
    } catch (error) {
      console.error('Error updating profile:', error);
      return false;
    }
  },

  updatePassword: async function (userId, currentPassword, newPassword) {
    try {
      // We should use a server-side API for password changes to handle proper password hashing
      // The direct approach below is insecure and doesn't work with hashed passwords

      // Make a POST request to the server endpoint for password change
      const response = await fetch('/Account/UpdateSecurity', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: new URLSearchParams({
          currentPassword: currentPassword,
          newPassword: newPassword,
          confirmPassword: newPassword
        })
      });

      const result = await response.json();
      return result;
    } catch (error) {
      console.error('Error updating password:', error);
      return { success: false, message: 'Error: ' + error.message };
    }
  }
};
