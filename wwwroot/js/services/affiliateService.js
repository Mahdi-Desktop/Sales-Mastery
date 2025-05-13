// wwwroot/js/services/affiliateService.js
const affiliateService = {
  // Dashboard data
  getDashboardData: async function (userId) {
    try {
      // Get affiliate record
      const affiliate = await firebaseService.getAffiliateByUserId(userId);
      if (!affiliate) return null;

      // Get commissions
      const commissions = await firebaseService.getCommissionsByAffiliateId(affiliate.affiliateId);

      // Get customers
      const customers = await firebaseService.getCustomersByAffiliateId(affiliate.affiliateId);

      // Calculate summary
      const totalCommission = firebaseService.getTotalCommission(commissions);
      const unpaidCommission = firebaseService.getUnpaidCommission(commissions);
      const paidCommission = totalCommission - unpaidCommission;
      const monthlyCommissions = firebaseService.getCommissionsByMonth(commissions);

      return {
        affiliate,
        commissions: commissions.slice(0, 5), // Most recent 5
        customers: customers.slice(0, 5), // First 5 customers
        customerCount: customers.length,
        totalCommission,
        unpaidCommission,
        paidCommission,
        monthlyCommissions
      };
    } catch (error) {
      console.error('Error getting dashboard data:', error);
      return null;
    }
  },

  // Commissions data
  getAllCommissions: async function (userId) {
    try {
      const affiliate = await firebaseService.getAffiliateByUserId(userId);
      if (!affiliate) return [];

      return await firebaseService.getCommissionsByAffiliateId(affiliate.affiliateId);
    } catch (error) {
      console.error('Error getting all commissions:', error);
      return [];
    }
  },

  // Customers data
  getAllCustomers: async function (userId) {
    try {
      const affiliate = await firebaseService.getAffiliateByUserId(userId);
      if (!affiliate) return [];

      return await firebaseService.getCustomersByAffiliateId(affiliate.affiliateId);
    } catch (error) {
      console.error('Error getting all customers:', error);
      return [];
    }
  }
};
