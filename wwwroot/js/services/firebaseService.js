const firebaseService = {
  // Initialize Firebase
  init: function (config) {
    if (!firebase.apps.length) {
      firebase.initializeApp(config);
    }
    this.db = firebase.firestore();
    return this;
  },

  // Authentication
  getCurrentUser: function () {
    return firebase.auth().currentUser;
  },

  // Affiliate Methods
  getAffiliateByUserId: async function (userId) {
    try {
      console.log('Looking for affiliate with userId:', userId);

      // First check the users collection with Role == '2' (affiliate) - this is the primary way to find affiliates
      let snapshot = await this.db
        .collection('users')
        .where('Role', '==', '2') // Role 2 for affiliates
        .where(firebase.firestore.FieldPath.documentId(), '==', userId)
        .limit(1)
        .get();

      // If not found by query, try direct document lookup
      if (snapshot.empty) {
        console.log('Not found by role query, trying direct document lookup');

        try {
          const userDoc = await this.db.collection('users').doc(userId).get();

          if (userDoc.exists) {
            const userData = userDoc.data();
            console.log('Found user document:', userData);

            if (userData.Role === '2') {
              return { affiliateId: userDoc.id, ...userData };
            }
          }
        } catch (directLookupError) {
          console.warn('Direct document lookup error:', directLookupError);
        }
      } else {
        const doc = snapshot.docs[0];
        console.log('Found affiliate record in users collection:', doc.data());
        return { affiliateId: doc.id, ...doc.data() };
      }

      // As a fallback, check if there's a dedicated affiliates collection
      snapshot = await this.db.collection('affiliates').where('UserId', '==', userId).limit(1).get();

      if (!snapshot.empty) {
        const doc = snapshot.docs[0];
        console.log('Found in affiliates collection as fallback:', doc.data());
        return { affiliateId: doc.id, ...doc.data() };
      }

      // As a last resort, assume the userId itself is the affiliateId if we know this is an affiliate
      console.log('No affiliate record found, trying userId as affiliateId directly');
      return { affiliateId: userId };
    } catch (error) {
      console.error('Error getting affiliate:', error);
      return null;
    }
  },

  getCommissionsByAffiliateId: async function (affiliateId) {
    try {
      console.log('Getting commissions for affiliateId:', affiliateId);

      // Try multiple field names for affiliate ID reference
      const possibleAffiliateFields = ['AffiliateId', 'affiliateId', 'ReferenceId', 'referenceId', 'UserId'];
      let allCommissions = [];

      // Try with "commissions" collection
      try {
        console.log('Querying commissions collection');
        // Try each possible field name
        for (const field of possibleAffiliateFields) {
          console.log(`Trying field: ${field}`);
          try {
            const snapshot = await this.db
              .collection('commissions')
              .where(field, '==', affiliateId)
              .limit(50) // Limit to 50 for performance
              .get();

            if (!snapshot.empty) {
              console.log(`Found commissions with field ${field}, count:`, snapshot.size);
              allCommissions = snapshot.docs.map(doc => ({ commissionId: doc.id, ...doc.data() }));
              break;
            }
          } catch (fieldError) {
            console.warn(`Error querying with field ${field}:`, fieldError);
          }
        }
      } catch (error) {
        console.warn('Error with commissions collection:', error);
      }

      // If nothing found, try with alternative collection name
      if (allCommissions.length === 0) {
        console.log('No commissions found, trying alternative collection name');
        try {
          for (const field of possibleAffiliateFields) {
            try {
              const snapshot = await this.db
                .collection('commisiond') // Alternative spelling
                .where(field, '==', affiliateId)
                .limit(50) // Limit to 50 for performance
                .get();

              if (!snapshot.empty) {
                console.log(`Found commissions in alternative collection with field ${field}, count:`, snapshot.size);
                allCommissions = snapshot.docs.map(doc => ({ commissionId: doc.id, ...doc.data() }));
                break;
              }
            } catch (fieldError) {
              console.warn(`Error querying alternative collection with field ${field}:`, fieldError);
            }
          }
        } catch (altError) {
          console.warn('Error with alternative commissions collection:', altError);
        }
      }

      // Sort by date if possible
      if (allCommissions.length > 0) {
        console.log('Total commissions found:', allCommissions.length);
        allCommissions.sort((a, b) => {
          // Try various date field names
          const dateFields = ['CreatedAt', 'createdAt', 'Date', 'date', 'OrderDate'];

          for (const field of dateFields) {
            if (a[field] && b[field]) {
              // Handle Firestore timestamps
              const dateA = a[field].toDate ? a[field].toDate() : new Date(a[field]);
              const dateB = b[field].toDate ? b[field].toDate() : new Date(b[field]);
              return dateB - dateA; // Newest first
            }
          }
          return 0;
        });
      } else {
        console.log('No commissions found for this affiliate');
      }

      return allCommissions;
    } catch (error) {
      console.error('Error getting commissions:', error);
      return [];
    }
  },

  getCustomersByAffiliateId: async function (affiliateId) {
    try {
      console.log('Getting customers for affiliateId:', affiliateId);

      // Try multiple field names for affiliate ID reference
      const possibleAffiliateFields = ['AffiliateId', 'affiliateId', 'ReferenceId', 'referenceId', 'RefererId'];
      let allCustomers = [];

      // First check users collection with Role == '3' (customer) - this is the primary way to find customers
      try {
        console.log('Querying users collection');
        for (const field of possibleAffiliateFields) {
          console.log(`Trying field: ${field}`);
          try {
            const snapshot = await this.db
              .collection('users')
              .where('Role', '==', '3') // Role 3 for customers
              .where(field, '==', affiliateId)
              .limit(50) // Limit to 50 for performance
              .get();

            if (!snapshot.empty) {
              console.log(`Found customers in users collection with field ${field}, count:`, snapshot.size);
              allCustomers = snapshot.docs.map(doc => ({
                customerId: doc.id,
                ...doc.data(),
                fullName: `${doc.data().FirstName || ''} ${doc.data().LastName || ''}`.trim() || 'Unknown'
              }));
              break;
            }
          } catch (fieldError) {
            console.warn(`Error querying users collection with field ${field}:`, fieldError);
          }
        }
      } catch (error) {
        console.warn('Error with users collection:', error);
      }

      // As a fallback, check if there's a dedicated customers collection
      if (allCustomers.length === 0) {
        console.log('No customers found in users collection, checking customers collection');
        try {
          for (const field of possibleAffiliateFields) {
            console.log(`Trying field: ${field}`);
            try {
              const snapshot = await this.db
                .collection('customers')
                .where(field, '==', affiliateId)
                .limit(50) // Limit to 50 for performance
                .get();

              if (!snapshot.empty) {
                console.log(`Found customers with field ${field}, count:`, snapshot.size);
                allCustomers = snapshot.docs.map(doc => ({
                  customerId: doc.id,
                  ...doc.data(),
                  fullName: `${doc.data().FirstName || ''} ${doc.data().LastName || ''}`.trim() || 'Unknown'
                }));
                break;
              }
            } catch (fieldError) {
              console.warn(`Error querying with field ${field}:`, fieldError);
            }
          }
        } catch (error) {
          console.warn('Error with customers collection:', error);
        }
      }

      console.log('Total customers found:', allCustomers.length);
      return allCustomers;
    } catch (error) {
      console.error('Error getting customers:', error);
      return [];
    }
  },

  getTotalCommission: function (commissions) {
    if (!commissions || !commissions.length) return 0;

    return commissions.reduce((sum, commission) => {
      // Try different field names
      const amount = commission.Amount || commission.amount || commission.Value || commission.value || 0;
      return sum + parseFloat(amount || 0);
    }, 0);
  },

  getUnpaidCommission: function (commissions) {
    if (!commissions || !commissions.length) return 0;

    return commissions
      .filter(commission => {
        // Check various field names for payment status
        const isPaid = commission.IsPaid || commission.isPaid || commission.Paid || commission.paid || false;
        return !isPaid;
      })
      .reduce((sum, commission) => {
        const amount = commission.Amount || commission.amount || commission.Value || commission.value || 0;
        return sum + parseFloat(amount || 0);
      }, 0);
  },

  getCommissionsByMonth: function (commissions) {
    const monthlyCommissions = {};

    if (!commissions || !commissions.length) return monthlyCommissions;

    commissions.forEach(commission => {
      // Try different date fields
      let date;
      const dateFields = ['CreatedAt', 'createdAt', 'Date', 'date', 'OrderDate', 'orderDate'];

      for (const field of dateFields) {
        if (commission[field]) {
          // Handle Firestore Timestamp
          date = commission[field].toDate ? commission[field].toDate() : new Date(commission[field]);
          break;
        }
      }

      if (!date) {
        console.warn('Commission missing date field:', commission);
        return; // Skip this commission
      }

      const month = date.getMonth() + 1;
      const year = date.getFullYear();
      const key = `${year}-${month}`;

      if (!monthlyCommissions[key]) {
        monthlyCommissions[key] = 0;
      }

      // Try different amount fields
      const amount = commission.Amount || commission.amount || commission.Value || commission.value || 0;
      monthlyCommissions[key] += parseFloat(amount || 0);
    });

    return monthlyCommissions;
  }
};
