/**
 * Table Row Click Handler
 * Makes table rows clickable and navigates to details page
 */

/** Clicks on row text may target a Text node, which has no .closest() */
function tableRowClickElement(e) {
    const t = e.target;
    if (!t) return null;
    return t.nodeType === Node.ELEMENT_NODE ? t : t.parentElement;
}

document.addEventListener('DOMContentLoaded', function() {
    // Initialize clickable rows for all tables
    const clickableRows = document.querySelectorAll('.clickable-row');
    
    clickableRows.forEach(row => {
        row.style.cursor = 'pointer';
        
        // Add hover effect
        row.addEventListener('mouseenter', function() {
            this.style.backgroundColor = 'rgba(0, 123, 255, 0.1)';
        });
        
        row.addEventListener('mouseleave', function() {
            this.style.backgroundColor = '';
        });
        
        // Add click handler
        row.addEventListener('click', function(e) {
            const el = tableRowClickElement(e);
            if (!el) return;
            // Don't navigate if clicking on buttons or links
            if (el.closest('button') || el.closest('a') || el.closest('.approval-actions')) {
                return;
            }
            
            const href = this.getAttribute('data-href');
            if (href) {
                window.location.href = href;
            }
        });
    });
});

