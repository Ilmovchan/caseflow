// Approval System JavaScript
// Handles approve/decline functionality for all entity types

document.addEventListener("DOMContentLoaded", function () {
  // Initialize approval system
  initializeApprovalSystem();
});

function initializeApprovalSystem() {
  // Handle approve button clicks
  document.addEventListener("click", function (e) {
    if (e.target.closest(".btn-approve")) {
      e.preventDefault();
      e.stopPropagation();
      const button = e.target.closest(".btn-approve");
      const id = button.getAttribute("data-id");
      const type = button.getAttribute("data-type");
      handleApproval(id, type, "approve");
    }

    // Handle reject button clicks
    if (e.target.closest(".btn-reject")) {
      e.preventDefault();
      e.stopPropagation();
      const button = e.target.closest(".btn-reject");
      const id = button.getAttribute("data-id");
      const type = button.getAttribute("data-type");
      handleApproval(id, type, "reject");
    }
  });
}

async function handleApproval(id, type, action) {
  // Prepare references and original content up-front so catch can restore UI
  const row = document.querySelector(`[data-id="${id}"]`).closest("tr");
  const actionCell = row.querySelector(".approval-actions");
  const originalContent = actionCell ? actionCell.innerHTML : "";
  try {
    // Show loading state
    if (actionCell) {
      actionCell.innerHTML =
        '<span class="text-muted"><i class="fas fa-spinner fa-spin"></i> Обробка...</span>';
    }

    // Determine the correct endpoint based on type
    let endpoint;
    const isDetectivePage = window.location.pathname.includes("/Detective/");

    if (isDetectivePage) {
      // Detective panel endpoints
      if (type === "report") {
        endpoint = `/Detective/Reports?handler=${
          action === "approve" ? "Approve" : "Reject"
        }&id=${id}`;
      } else if (type === "expense") {
        endpoint = `/Detective/Expenses?handler=${
          action === "approve" ? "Approve" : "Reject"
        }&id=${id}`;
      } else {
        throw new Error(`Unknown entity type: ${type}`);
      }
    } else {
      // Admin panel endpoints
      if (type === "expense") {
        endpoint = `/api/admin/expense/${id}/${action}`;
      } else if (type === "report") {
        endpoint = `/api/admin/report/${id}/${action}`;
      } else if (type === "evidence") {
        endpoint = `/api/admin/evidence/${id}/${action}`;
      } else if (type === "suspect") {
        endpoint = `/api/admin/suspect/${id}/${action}`;
      } else {
        throw new Error(`Unknown entity type: ${type}`);
      }
    }

    console.log(`Making request to: ${endpoint}`);

    // Make the API call
    const response = await fetch(endpoint, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      credentials: "include",
    });

    console.log(`Response status: ${response.status}`);

    if (!response.ok) {
      const errorText = await response.text();
      console.error(`HTTP error response: ${errorText}`);
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const result = await response.json();
    console.log("Response data:", result);

    // Handle delete/reject action (removes row)
    if (action === "reject" && result.success && result.message) {
      // Remove the row from the table
      row.remove();
      showNotification(result.message, "success");
    } else {
      // Update the UI for submit/approve action
      updateApprovalStatus(
        row,
        result.newStatus,
        result.newStatusText,
        result.newStatusColor
      );

      // Show success message
      const isDetectivePage = window.location.pathname.includes("/Detective/");
      const message = isDetectivePage
        ? result.message || "Надіслано на перевірку успішно!"
        : `${action === "approve" ? "Схвалено" : "Відхилено"} успішно!`;

      showNotification(message, "success");
    }
  } catch (error) {
    console.error("Approval error:", error);
    // Restore original content
    if (actionCell) {
      actionCell.innerHTML = originalContent;
    }

    // Show error message
    showNotification(
      `Помилка при ${action === "approve" ? "схваленні" : "відхиленні"}: ${
        error.message
      }`,
      "error"
    );
  }
}

function updateApprovalStatus(row, newStatus, newStatusText, newStatusColor) {
  // Update the status badge
  const statusCell = row.querySelector(".approval-status");
  if (statusCell) {
    statusCell.className = `badge bg-${newStatusColor} approval-status`;
    statusCell.textContent = newStatusText;
    statusCell.setAttribute("data-status", newStatus);
  }

  // Update the action buttons
  const actionCell = row.querySelector(".approval-actions");
  if (actionCell) {
    actionCell.innerHTML = '<span class="text-muted">Оброблено</span>';
  }
}

function getAntiForgeryToken() {
  const token = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  );
  return token ? token.value : "";
}

function showNotification(message, type = "info") {
  // Create notification element
  const notification = document.createElement("div");
  notification.className = `alert alert-${
    type === "error" ? "danger" : type
  } alert-dismissible fade show position-fixed`;
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

  // Add to page
  document.body.appendChild(notification);

  // Auto-remove after 5 seconds
  setTimeout(() => {
    if (notification.parentNode) {
      notification.remove();
    }
  }, 5000);
}
