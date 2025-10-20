/**
 * Table Row Click Handler
 * Makes table rows clickable and navigates to details page
 */

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
            // Don't navigate if clicking on buttons or links
            if (e.target.closest('button') || e.target.closest('a') || e.target.closest('.approval-actions')) {
                return;
            }
            
            const href = this.getAttribute('data-href');
            if (href) {
                window.location.href = href;
            }
        });
    });
});

