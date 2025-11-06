// Logout functionality for Admin Dashboard
document.addEventListener("DOMContentLoaded", function () {
  const logoutForm = document.getElementById("logoutForm");

  if (logoutForm) {
    logoutForm.addEventListener("submit", function (event) {
      event.preventDefault();

      if (!confirm("Do you want to logout?")) {
        return;
      }

      // Send logout request
      fetch("/admin/logout", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
      })
        .then((response) => {
          if (response.ok) {
            return response.json();
          }
          throw new Error("Logout failed");
        })
        .then((data) => {
          if (data.logout) {
            console.log("✅ Logout successful");
            // Redirect to login page
            window.location.href = "/admin/login";
          } else {
            alert("Logout failed. Please try again.");
          }
        })
        .catch((error) => {
          console.error("❌ Logout error:", error);
          alert("An error occurred during logout");
        });
    });
  }
});
