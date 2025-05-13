const customerService = {
  getCustomersByAffiliateId: async function (affiliateId) {
    try {
      const snapshot = await firebaseService.db.collection('customers').where('AffiliateId', '==', affiliateId).get();

      return snapshot.docs.map(doc => ({
        customerId: doc.id,
        ...doc.data(),
        fullName: `${doc.data().FirstName || ''} ${doc.data().LastName || ''}`.trim(),
        formattedDate: doc.data().CreatedAt ? doc.data().CreatedAt.toDate().toLocaleDateString() : 'N/A'
      }));
    } catch (error) {
      console.error('Error getting customers:', error);
      return [];
    }
  },

  getOrdersByCustomerId: async function (customerId) {
    try {
      const snapshot = await firebaseService.db
        .collection('orders')
        .where('CustomerId', '==', customerId)
        .orderBy('OrderDate', 'desc')
        .get();

      return snapshot.docs.map(doc => ({
        orderId: doc.id,
        ...doc.data(),
        formattedDate: doc.data().OrderDate ? doc.data().OrderDate.toDate().toLocaleDateString() : 'N/A'
      }));
    } catch (error) {
      console.error('Error getting orders:', error);
      return [];
    }
  },

  getCustomerStats: async function (affiliateId) {
    try {
      const customers = await this.getCustomersByAffiliateId(affiliateId);

      // Get total number of customers
      const total = customers.length;

      // Count customers by month
      const customersByMonth = {};
      customers.forEach(customer => {
        if (!customer.CreatedAt) return;

        const date = customer.CreatedAt.toDate();
        const month = date.getMonth();
        const year = date.getFullYear();
        const key = `${year}-${month}`;

        if (!customersByMonth[key]) {
          customersByMonth[key] = 0;
        }

        customersByMonth[key]++;
      });

      return { total, customersByMonth };
    } catch (error) {
      console.error('Error getting customer stats:', error);
      return { total: 0, customersByMonth: {} };
    }
  }
};
