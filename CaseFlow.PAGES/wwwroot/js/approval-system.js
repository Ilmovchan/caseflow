// Approval System JavaScript
// Handles approve/decline functionality for all entity types

/** Clicks on text inside a <button> often set event.target to a Text node, which has no .closest() */
function eventTargetElement(event) {
  const t = event.target;
  if (!t) return null;
  return t.nodeType === Node.ELEMENT_NODE ? t : t.parentElement;
}

function bootApprovalSystem() {
  initializeApprovalSystem();
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", bootApprovalSystem);
} else {
  bootApprovalSystem();
}

function initializeApprovalSystem() {
  if (window.__caseflowApprovalSystemBound) return;
  window.__caseflowApprovalSystemBound = true;

  document.addEventListener("click", function (e) {
    const fromEl = eventTargetElement(e);
    if (!fromEl) return;

    if (fromEl.closest(".btn-approve")) {
      e.preventDefault();
      e.stopPropagation();
      const button = fromEl.closest(".btn-approve");
      const type = button.getAttribute("data-type");
      handleApproval(button, type, "approve");
      return;
    }

    if (fromEl.closest(".btn-reject")) {
      e.preventDefault();
      e.stopPropagation();
      const button = fromEl.closest(".btn-reject");
      const type = button.getAttribute("data-type");
      handleApproval(button, type, "reject");
    }
  });
}

/** POST URL for the current list page (respects PathBase and avoids hard-coded /Admin/... roots). */
function buildHandlerUrl(handler, id) {
  const path = window.location.pathname.replace(/\/$/, "") || "/";
  const params = new URLSearchParams();
  params.set("handler", handler);
  params.set("id", String(id));
  return `${path}?${params.toString()}`;
}

async function handleApproval(button, type, action) {
  const id = button.getAttribute("data-id");
  const row = button.closest("tr");
  if (!row) {
    console.error("approval-system: could not find table row for button");
    return;
  }
  const actionCell = row.querySelector(".approval-actions");
  const originalContent = actionCell ? actionCell.innerHTML : "";
  const isEvidence = type === "evidence";
  try {
    if (actionCell) {
      actionCell.innerHTML =
        '<span class="text-muted"><i class="fas fa-spinner fa-spin"></i> Обробка...</span>';
    }

    let endpoint;
    const isDetectivePage = window.location.pathname.includes("/Detective/");

    const statusBadge = row.querySelector(".approval-status");
    const isDraft =
      statusBadge && statusBadge.getAttribute("data-status") === "Draft";

    if (isDetectivePage) {
      const handler = action === "approve" ? "Approve" : "Reject";
      if (type === "report") {
        endpoint = buildHandlerUrl(handler, id);
      } else if (type === "expense") {
        endpoint = buildHandlerUrl(handler, id);
      } else if (type === "evidence") {
        endpoint = buildHandlerUrl(handler, id);
      } else if (type === "suspect") {
        endpoint = buildHandlerUrl(handler, id);
      } else {
        throw new Error(`Unknown entity type: ${type}`);
      }
    } else {
      let handler;
      if (type === "evidence") {
        handler = action === "approve" ? "Approve" : "Reject";
      } else {
        handler = isDraft
          ? action === "approve"
            ? "Submit"
            : "Delete"
          : action === "approve"
            ? "Approve"
            : "Reject";
      }

      if (
        type === "expense" ||
        type === "report" ||
        type === "evidence" ||
        type === "suspect"
      ) {
        endpoint = buildHandlerUrl(handler, id);
      } else {
        throw new Error(`Unknown entity type: ${type}`);
      }
    }

    console.log(`Making request to: ${endpoint}`);

    const response = await fetch(endpoint, {
      method: "POST",
      credentials: "include",
      headers: {
        Accept: "application/json",
      },
    });

    console.log(`Response status: ${response.status}`);

    if (!response.ok) {
      const errorText = await response.text();
      console.error(`HTTP error response: ${errorText}`);
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const result = await response.json();
    console.log("Response data:", result);

    if (result && result.success === false) {
      throw new Error(result.error || "Операція не вдалася");
    }

    const isDetectivePage2 = window.location.pathname.includes("/Detective/");

    // Remove row only when server signals a delete-style action with a message (e.g. draft delete)
    if (action === "reject" && result.success && result.message) {
      row.remove();
      showNotification(result.message, "success");
    } else {
      updateApprovalStatus(
        row,
        result.newStatus,
        result.newStatusText,
        result.newStatusColor
      );

      let message;

      if (isDetectivePage2) {
        message = result.message || "Надіслано на перевірку успішно!";
      } else {
        if (isEvidence) {
          message = `${action === "approve" ? "Схвалено" : "Відхилено"} успішно!`;
        } else if (isDraft && action === "approve") {
          message = result.message || "Надіслано на перевірку успішно!";
        } else {
          message = `${action === "approve" ? "Схвалено" : "Відхилено"} успішно!`;
        }
      }

      showNotification(message, "success");
    }
  } catch (error) {
    console.error("Approval error:", error);
    if (actionCell) {
      actionCell.innerHTML = originalContent;
    }

    showNotification(
      `Помилка при ${action === "approve" ? "схваленні" : "відхиленні"}: ${
        error.message
      }`,
      "error"
    );
  }
}

function updateApprovalStatus(row, newStatus, newStatusText, newStatusColor) {
  const statusCell = row.querySelector(".approval-status");
  if (statusCell) {
    statusCell.className = `badge bg-${newStatusColor} approval-status`;
    statusCell.textContent = newStatusText;
    statusCell.setAttribute("data-status", newStatus);
  }

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
  const notification = document.createElement("div");
  notification.className = `alert alert-${
    type === "error" ? "danger" : type
  } alert-dismissible show position-fixed`;
  notification.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px; opacity: 1 !important;";
  notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

  document.body.appendChild(notification);

  setTimeout(() => {
    if (notification.parentNode) {
      notification.remove();
    }
  }, 5000);
}
