// === User Management Logic === //
document.addEventListener("DOMContentLoaded", () => {
  const searchInput = document.getElementById("user-search");
  const tableBody = document.getElementById("users-table-body");

  if (!tableBody) return;
  const tableRows = tableBody.querySelectorAll("tr");

  // 🔍 Search filter
  if (searchInput) {
    searchInput.addEventListener("input", function () {
      const keyword = this.value.toLowerCase().trim();
      tableRows.forEach((row) => {
        const name = row.children[0].innerText.toLowerCase();
        const phone = row.children[1].innerText.toLowerCase();
        const email = row.children[2].innerText.toLowerCase();
        const visible =
          name.includes(keyword) ||
          phone.includes(keyword) ||
          email.includes(keyword);
        row.style.display = visible ? "" : "none";
      });
    });
  }

  // 🧩 Helper: show nice animated toast (instead of alert)
  const showToast = (message, type = "info") => {
    const toast = document.createElement("div");
    toast.className = `status-toast ${type}`;
    toast.innerHTML = message;
    document.body.appendChild(toast);

    setTimeout(() => toast.classList.add("visible"), 50);
    setTimeout(() => {
      toast.classList.remove("visible");
      setTimeout(() => toast.remove(), 300);
    }, 2200);
  };

  // ✅ Handle member status updates
  document.querySelectorAll(".member-status-btn").forEach((dropdown) => {
    dropdown.addEventListener("change", async function () {
      const newStatus = this.value;
      const userId = this.dataset.id;

      if (!userId) {
        showToast("⚠️ Missing member ID.", "warning");
        return;
      }

      try {
        const res = await fetch("/admin/update-member", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            Id: userId,
            MemberStatus: newStatus.toUpperCase(), // send enum-compatible uppercase string
          }),
        });

        const data = await res.json();
        if (data.success) {
          this.className = `member-status-btn ${newStatus.toLowerCase()}`;
          showToast("✅ User status updated successfully!", "success");
        } else {
          showToast("❌ Failed to update user status.", "danger");
        }
      } catch (err) {
        console.error("Error updating user:", err);
        showToast("⚠️ Network error occurred.", "warning");
      }
    });
  });
});
