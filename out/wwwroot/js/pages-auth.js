/**
 *  Pages Authentication
 */

'use strict';
// Only define formAuthentication if we're on a page with this element
const formAuthentication = document.querySelector('#formAuthentication');

// ====== Reset Password Multi-Step Process with Firebase ======
let currentStep = 1;
let firebaseVerificationId = ''; // Store the Firebase verification ID
let recaptchaVerifier = null;

// Wait for Firebase to be initialized before setting up reset password
document.addEventListener('firebase-ready', function () {
  console.log("Firebase ready event received in pages-auth.js");
  initializeResetPassword();
});

// Document ready handler to check if Firebase is already available
document.addEventListener('DOMContentLoaded', function () {
  if (document.getElementById('step1')) {
    console.log("Reset password form detected");

    // Check if Firebase is already initialized
    if (window.firebaseApp) {
      console.log("Firebase already initialized, setting up reset password");
      initializeResetPassword();
    } else {
      console.log("Waiting for Firebase to be initialized...");
      // We'll wait for the 'firebase-ready' event
    }
  }
});

// Form validation setup
document.addEventListener('DOMContentLoaded', function (e) {
  // Only run FormValidation if the form exists
  if (formAuthentication) {
    const fv = FormValidation.formValidation(formAuthentication, {
      fields: {
        username: {
          validators: {
            notEmpty: {
              message: 'Please enter username'
            },
            stringLength: {
              min: 6,
              message: 'Username must be more than 6 characters'
            }
          }
        },
        email: {
          validators: {
            notEmpty: {
              message: 'Please enter your email'
            },
            emailAddress: {
              message: 'Please enter valid email address'
            }
          }
        },
        'email-username': {
          validators: {
            notEmpty: {
              message: 'Please enter email / username'
            },
            stringLength: {
              min: 6,
              message: 'Username must be more than 6 characters'
            }
          }
        },
        password: {
          validators: {
            notEmpty: {
              message: 'Please enter your password'
            },
            stringLength: {
              min: 6,
              message: 'Password must be more than 6 characters'
            }
          }
        },
        'confirm-password': {
          validators: {
            notEmpty: {
              message: 'Please confirm password'
            },
            identical: {
              compare: function () {
                return formAuthentication.querySelector('[name="password"]').value;
              },
              message: 'The password and its confirm are not the same'
            },
            stringLength: {
              min: 6,
              message: 'Password must be more than 6 characters'
            }
          }
        },
        terms: {
          validators: {
            notEmpty: {
              message: 'Please agree terms & conditions'
            }
          }
        }
      },
      plugins: {
        trigger: new FormValidation.plugins.Trigger(),
        bootstrap5: new FormValidation.plugins.Bootstrap5({
          eleValidClass: '',
          rowSelector: '.mb-6'
        }),
        submitButton: new FormValidation.plugins.SubmitButton(),
        defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
        autoFocus: new FormValidation.plugins.AutoFocus()
      },
      init: instance => {
        instance.on('plugins.message.placed', function (e) {
          if (e.element.parentElement.classList.contains('input-group')) {
            e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
          }
        });
      }
    });
  }
});

// Initialize reset password functionality
function initializeResetPassword() {
  if (!document.getElementById('step1')) return;

  // Initialize the recaptchaVerifier
  if (typeof firebase !== 'undefined' && firebase.auth) {
    console.log("Setting up reCAPTCHA for phone auth");

    try {
      // Create the reCAPTCHA verifier with the correct container ID
      recaptchaVerifier = new firebase.auth.RecaptchaVerifier('recaptcha-container', {
        'size': 'normal',
        'callback': (response) => {
          console.log("reCAPTCHA verified with token:", response.length, "chars");
          document.getElementById('phoneSubmitBtn').disabled = false;
        },
        'expired-callback': () => {
          console.log("reCAPTCHA expired");
          document.getElementById('phoneSubmitBtn').disabled = true;
        }
      });

      // Check if the container exists before rendering
      const container = document.getElementById('recaptcha-container');
      if (container) {
        console.log("Found reCAPTCHA container, rendering reCAPTCHA");
        recaptchaVerifier.render()
          .then(widgetId => {
            console.log("reCAPTCHA rendered with widget ID:", widgetId);
            window.recaptchaWidgetId = widgetId;
          })
          .catch(error => {
            console.error("Error rendering reCAPTCHA:", error);
          });
      } else {
        console.error("reCAPTCHA container not found!");
      }
    } catch (error) {
      console.error("Error initializing reCAPTCHA:", error);
    }
  } else {
    console.error("Firebase Auth is not available!");
  }

  // Setup event handlers and show first step
  setupResetPasswordEventHandlers();
  showCurrentStep();
}

// Setup all event handlers
function setupResetPasswordEventHandlers() {
  // Phone number submission
  const phoneForm = document.getElementById('phoneForm');
  const phoneSubmitBtn = document.getElementById('phoneSubmitBtn');

  if (phoneForm) {
    phoneForm.addEventListener('submit', function (e) {
      e.preventDefault();
      submitPhoneNumber();
    });
  }

  if (phoneSubmitBtn) {
    phoneSubmitBtn.addEventListener('click', function (e) {
      e.preventDefault();
      submitPhoneNumber();
    });
  }

  // OTP verification
  const otpForm = document.getElementById('otpForm');
  const otpSubmitBtn = document.getElementById('otpSubmitBtn');

  if (otpForm) {
    otpForm.addEventListener('submit', function (e) {
      e.preventDefault();
      verifyOTP();
    });
  }

  if (otpSubmitBtn) {
    otpSubmitBtn.addEventListener('click', function (e) {
      e.preventDefault();
      verifyOTP();
    });
  }

  // Password reset
  const passwordForm = document.getElementById('passwordForm');
  const passwordSubmitBtn = document.getElementById('passwordSubmitBtn');

  if (passwordForm) {
    passwordForm.addEventListener('submit', function (e) {
      e.preventDefault();
      resetPassword();
    });
  }

  if (passwordSubmitBtn) {
    passwordSubmitBtn.addEventListener('click', function (e) {
      e.preventDefault();
      resetPassword();
    });
  }
}

// Show the current step in the reset password flow
function showCurrentStep() {
  console.log("Showing step:", currentStep);

  // Hide all steps
  const steps = document.querySelectorAll('.reset-step');
  steps.forEach(step => {
    step.style.display = 'none';
  });

  // Show current step
  const currentStepElement = document.getElementById(`step${currentStep}`);
  if (currentStepElement) {
    currentStepElement.style.display = 'block';
  }

  // If moving away from step 1, hide reCAPTCHA container
  // This ensures reCAPTCHA only shows in step 1
  const recaptchaContainer = document.getElementById('recaptcha-container');
  if (recaptchaContainer) {
    if (currentStep === 1) {
      recaptchaContainer.style.display = 'block';
    } else {
      recaptchaContainer.style.display = 'none';
    }
  }

  // Update progress indicators
  updateProgress();
}

// Update the progress indicators
function updateProgress() {
  const progressItems = document.querySelectorAll('.progress-item');
  progressItems.forEach(item => {
    const stepNumber = parseInt(item.getAttribute('data-step'));

    item.classList.remove('active', 'completed');

    if (stepNumber < currentStep) {
      item.classList.add('completed');
    } else if (stepNumber === currentStep) {
      item.classList.add('active');
    }
  });
}

// Submit phone number for Firebase verification
function submitPhoneNumber() {
  console.log("submitPhoneNumber function called");

  const phoneInput = document.getElementById('phone');
  if (!phoneInput) {
    console.error("Phone input element not found");
    return;
  }

  let phoneNumber = phoneInput.value.trim();
  console.log("Submitting phone number:", phoneNumber);

  if (!phoneNumber) {
    alert('Please enter your phone number');
    return;
  }

  const phoneSubmitBtn = document.getElementById('phoneSubmitBtn');
  if (phoneSubmitBtn) {
    phoneSubmitBtn.disabled = true;
    phoneSubmitBtn.innerHTML = 'Sending...';
  }

  // Format phone number - always ensure it starts with +961
  // Remove any spaces or dashes
  phoneNumber = phoneNumber.replace(/[\s-]/g, '');

  // Remove any leading 0
  if (phoneNumber.startsWith('0')) {
    phoneNumber = phoneNumber.substring(1);
  }

  // If it doesn't have the country code, add it
  if (!phoneNumber.startsWith('+961') && !phoneNumber.startsWith('961')) {
    phoneNumber = '+961' + phoneNumber;
  } else if (phoneNumber.startsWith('961')) {
    phoneNumber = '+' + phoneNumber;
  }

  console.log("Formatted phone number:", phoneNumber);

  // First check if the phone number exists in our database
  fetch('/Auth/CheckPhoneExists', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
    },
    body: `phoneNumber=${encodeURIComponent(phoneNumber)}`
  })
    .then(response => response.json())
    .then(data => {
      if (data.exists) {
        // Phone exists in database, now try Firebase auth
        console.log("Phone found in database, proceeding with Firebase auth");

        // If recaptchaVerifier was cleared, create a new invisible one
        if (!recaptchaVerifier || recaptchaVerifier._deleted) {
          try {
            recaptchaVerifier = new firebase.auth.RecaptchaVerifier('phoneSubmitBtn', {
              'size': 'invisible'
            });
            console.log("Created new invisible reCAPTCHA");
          } catch (error) {
            console.error("Error creating invisible reCAPTCHA:", error);
            alert("Error with verification. Please refresh the page and try again.");
            return;
          }
        }

        // Now use Firebase for phone auth
        firebase.auth().signInWithPhoneNumber(phoneNumber, recaptchaVerifier)
          .then(confirmationResult => {
            console.log("SMS sent successfully");
            window.confirmationResult = confirmationResult;

            // Store Firebase verification token in server session
            fetch('/Auth/StoreFirebaseVerification', {
              method: 'POST',
              headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
              },
              body: `token=${confirmationResult.verificationId}`
            })
              .then(response => response.json())
              .then(data => {
                console.log("Firebase verification token stored:", data);
              })
              .catch(error => {
                console.error("Error storing verification token:", error);
              });

            currentStep = 2;
            showCurrentStep();
          })
          .catch(error => {
            console.error("Error sending SMS:", error);

            // Special handling for common Firebase errors
            let errorMessage = "Error sending verification code: " + error.message;

            if (error.code === 'auth/invalid-phone-number') {
              errorMessage = "Invalid phone number format. Please enter a valid Lebanese phone number.";
            } else if (error.code === 'auth/quota-exceeded') {
              errorMessage = "Too many verification attempts. Please try again later.";
            } else if (error.code === 'auth/captcha-check-failed') {
              errorMessage = "reCAPTCHA verification failed. Please refresh the page and try again.";
            }

            alert(errorMessage);

            // Reset the button
            if (phoneSubmitBtn) {
              phoneSubmitBtn.disabled = false;
              phoneSubmitBtn.innerHTML = 'Submit';
            }
          });
      } else {
        alert(data.message || "Phone number not found in our records");
        if (phoneSubmitBtn) {
          phoneSubmitBtn.disabled = false;
          phoneSubmitBtn.innerHTML = 'Submit';
        }
      }
    })
    .catch(error => {
      console.error("Error checking phone:", error);
      alert("Error connecting to server. Please try again.");
      if (phoneSubmitBtn) {
        phoneSubmitBtn.disabled = false;
        phoneSubmitBtn.innerHTML = 'Submit';
      }
    });
}

// Verify OTP code
function verifyOTP() {
  console.log("verifyOTP function called");

  const otpInput = document.getElementById('otp');
  if (!otpInput) {
    console.error("OTP input element not found");
    return;
  }

  const otp = otpInput.value.trim();
  console.log("Verifying OTP:", otp);

  if (!otp) {
    alert('Please enter the verification code');
    return;
  }

  const otpSubmitBtn = document.getElementById('otpSubmitBtn');
  if (otpSubmitBtn) {
    otpSubmitBtn.disabled = true;
    otpSubmitBtn.innerHTML = 'Verifying...';
  }

  if (!window.confirmationResult) {
    console.error("No confirmation result available");
    alert("Verification session expired. Please try again.");
    currentStep = 1;
    showCurrentStep();
    if (otpSubmitBtn) {
      otpSubmitBtn.disabled = false;
      otpSubmitBtn.innerHTML = 'Verify';
    }
    return;
  }

  console.log("Confirming OTP with Firebase...");
  window.confirmationResult.confirm(otp)
    .then(result => {
      console.log("Phone authentication successful!");
      const user = result.user;
      console.log("Authenticated user:", user.uid);

      // Move to step 3
      currentStep = 3;
      showCurrentStep();

      if (otpSubmitBtn) {
        otpSubmitBtn.disabled = false;
        otpSubmitBtn.innerHTML = 'Verify';
      }
    })
    .catch(error => {
      console.error("Error verifying code:", error);
      alert("Invalid verification code. Please try again.");
      if (otpSubmitBtn) {
        otpSubmitBtn.disabled = false;
        otpSubmitBtn.innerHTML = 'Verify';
      }
    });
}

// Reset password with new password
function resetPassword() {
  console.log("resetPassword function called");

  const passwordInput = document.getElementById('password');
  const confirmPasswordInput = document.getElementById('confirm-password');

  if (!passwordInput || !confirmPasswordInput) {
    console.error("Password input elements not found");
    return;
  }

  const password = passwordInput.value;
  const confirmPassword = confirmPasswordInput.value;

  if (password !== confirmPassword) {
    alert("Passwords don't match");
    return;
  }

  if (password.length < 6) {
    alert('Password must be at least 6 characters long');
    return;
  }

  const passwordSubmitBtn = document.getElementById('passwordSubmitBtn');
  if (passwordSubmitBtn) {
    passwordSubmitBtn.disabled = true;
    passwordSubmitBtn.innerHTML = 'Processing...';
  }

  // Get Firebase user if available
  const firebaseToken = firebase.auth().currentUser?.uid || '';
  console.log("Using Firebase user ID:", firebaseToken);

  // Call your server endpoint to update the password
  fetch('/Auth/ResetPassword', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
      'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
    },
    body: `newPassword=${encodeURIComponent(password)}&firebaseToken=${encodeURIComponent(firebaseToken)}`
  })
    .then(response => {
      console.log("Server response status:", response.status);
      return response.json();
    })
    .then(data => {
      console.log("Password reset response:", data);
      if (data.success) {
        alert('Password reset successful! You will be redirected to the login page.');
        window.location.href = '/Auth/LoginBasic';
      } else {
        alert(data.message || 'Failed to reset password');
      }
    })
    .catch(error => {
      console.error("Error resetting password:", error);
      alert('Network error. Please try again.');
    })
    .finally(() => {
      if (passwordSubmitBtn) {
        passwordSubmitBtn.disabled = false;
        passwordSubmitBtn.innerHTML = 'Set new password';
      }
    });
}

// Function to go back to a specific step
window.goToStep = function (step) {
  console.log("Going to step:", step);

  // Special handling for going back to step 1
  if (step === 1 && currentStep !== 1) {
    console.log("Going back to step 1, reloading page for fresh reCAPTCHA");
    window.location.reload();
    return;
  }

  currentStep = step;
  showCurrentStep();
};

