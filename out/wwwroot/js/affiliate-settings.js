/**
 * Affiliate Program Settings
 */

'use strict';

document.addEventListener('DOMContentLoaded', function () {
  // Check if user is admin
  if (!isUserAdmin()) {
    window.location.href = '/Home/AccessDenied';
    return;
  }

  // Wait for Firebase to be ready
  if (window.db) {
    initializeAffiliateSettings(window.db);
  } else {
    document.addEventListener('firebase-ready', function (e) {
      initializeAffiliateSettings(e.detail.db);
    });
  }
});

function initializeAffiliateSettings(db) {
  // Show loading state
  const form = document.getElementById('affiliateSettingsForm');
  if (form) {
    form.innerHTML = `
      <div class="text-center py-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading settings...</span>
        </div>
        <p class="mt-2">Loading affiliate program settings...</p>
      </div>
    `;
  }

  // Load settings from Firestore
  db.collection('settings').doc('affiliate-settings').get()
    .then(doc => {
      if (doc.exists) {
        const settings = doc.data();
        populateSettingsForm(settings);
      } else {
        // Create default settings if not exists
        const defaultSettings = {
          ProgramEnabled: true,
          AutoApprove: false,
          DefaultCommissionRate: 15,
          MinPayoutAmount: 50,
          CookieDuration: 30,
          PayoutSchedule: 'monthly',
          NotificationSettings: {
            NotifyNewReferral: true,
            NotifyNewCommission: true,
            NotifyPayout: true,
            AdminNotifications: true
          },
          TermsAndConditions: document.getElementById('termsAndConditions').value
        };

        // Save default settings
        db.collection('settings').doc('affiliate-settings').set(defaultSettings)
          .then(() => {
            populateSettingsForm(defaultSettings);
          })
          .catch(error => {
            console.error('Error creating default settings:', error);
            showError('Failed to create default settings');
          });
      }
    })
    .catch(error => {
      console.error('Error loading settings:', error);
      showError('Failed to load settings');
    });

  // Add save handlers
  const saveButtons = document.querySelectorAll('#saveSettings, #saveSettingsFooter');
  saveButtons.forEach(button => {
    button.addEventListener('click', function () {
      saveSettings(db, this);
    });
  });
}

// Populate settings form with data
function populateSettingsForm(settings) {
  const form = document.getElementById('affiliateSettingsForm');
  if (!form) return;

  // Restore original form HTML (you would need to have this in a variable or template)
  form.innerHTML = document.getElementById('settingsFormTemplate').innerHTML;

  // Set form values
  document.getElementById('programEnabled').checked = settings.ProgramEnabled;
  document.getElementById('autoApprove').checked = settings.AutoApprove;
  document.getElementById('defaultCommissionRate').value = settings.DefaultCommissionRate;
  document.getElementById('minPayoutAmount').value = settings.MinPayoutAmount;
  document.getElementById('cookieDuration').value = settings.CookieDuration;
  document.getElementById('payoutSchedule').value = settings.PayoutSchedule;

  // Notification settings
  if (settings.NotificationSettings) {
    document.getElementById('notifyNewReferral').checked = settings.NotificationSettings.NotifyNewReferral;
    document.getElementById('notifyNewCommission').checked = settings.NotificationSettings.NotifyNewCommission;
    document.getElementById('notifyPayout').checked = settings.NotificationSettings.NotifyPayout;
    document.getElementById('adminNotifications').checked = settings.NotificationSettings.AdminNotifications;
  }

  // Terms and conditions
  if (settings.TermsAndConditions) {
    document.getElementById('termsAndConditions').value = settings.TermsAndConditions;
  }
}

// Save settings to Firestore
function saveSettings(db, button) {
  // Show spinner on button
  const originalButtonHtml = button.innerHTML;
  button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...';
  button.disabled = true;

  // Get form values
  const settings = {
    ProgramEnabled: document.getElementById('programEnabled').checked,
    AutoApprove: document.getElementById('autoApprove').checked,
    DefaultCommissionRate: parseFloat(document.getElementById('defaultCommissionRate').value),
    MinPayoutAmount: parseFloat(document.getElementById('minPayoutAmount').value),
    CookieDuration: parseInt(document.getElementById('cookieDuration').value),
    PayoutSchedule: document.getElementById('payoutSchedule').value,
    NotificationSettings: {
      NotifyNewReferral: document.getElementById('notifyNewReferral').checked,
      NotifyNewCommission: document.getElementById('notifyNewCommission').checked,
      NotifyPayout: document.getElementById('notifyPayout').checked,
      AdminNotifications: document.getElementById('adminNotifications').checked
    },
    TermsAndConditions: document.getElementById('termsAndConditions').value
  };

  // Save to Firestore
  db.collection('settings').doc('affiliate-settings').set(settings)
    .then(() => {
      // Show success message
      Swal.fire({
        title: 'Settings Saved!',
        text: 'Your affiliate program settings have been updated successfully.',
        icon: 'success',
        customClass: {
          confirmButton: 'btn btn-success'
        },
        buttonsStyling: false
      });

      // Reset button state
      button.innerHTML = originalButtonHtml;
      button.disabled = false;
    })
    .catch(error => {
      console.error('Error saving settings:', error);

      // Show error message
      Swal.fire({
        title: 'Error!',
        text: 'Failed to save settings. Please try again.',
        icon: 'error',
        customClass: {
          confirmButton: 'btn btn-primary'
        },
        buttonsStyling: false
      });

      // Reset button state
      button.innerHTML = originalButtonHtml;
      button.disabled = false;
    });
}

// Show error message
function showError(message) {
  const form = document.getElementById('affiliateSettingsForm');
  if (!form) return;

  form.innerHTML = `
    <div class="alert alert-danger d-flex align-items-center" role="alert">
      <i class="ti ti-alert-circle me-2"></i>
      <div>
        <h6 class="alert-heading mb-1">Error</h6>
        <span>${message}</span>
      </div>
    </div>
  `;
}
