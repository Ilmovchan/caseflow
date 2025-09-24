/**
 * Table Sorting Functionality
 * Makes table headers clickable and enables client-side sorting
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize sorting for all tables with the 'sortable-table' class
    const tables = document.querySelectorAll('.sortable-table');
    tables.forEach(initTableSorting);
});

function initTableSorting(table) {
    const headers = table.querySelectorAll('thead th');
    
    headers.forEach((header, index) => {
        // Skip the last column if it contains action buttons
        const isActionColumn = header.textContent.trim() === '' || 
                              header.textContent.includes('Деталі') || 
                              header.textContent.includes('Редагувати') || 
                              header.textContent.includes('Видалити') ||
                              header.textContent.includes('Details') ||
                              header.textContent.includes('Edit') ||
                              header.textContent.includes('Delete');
        
        if (isActionColumn) return;
        
        // Make header clickable
        header.style.cursor = 'pointer';
        header.style.userSelect = 'none';
        header.style.position = 'relative';
        
        // Add hover effect
        header.addEventListener('mouseenter', function() {
            this.style.backgroundColor = '#f8f9fa';
        });
        
        header.addEventListener('mouseleave', function() {
            this.style.backgroundColor = '';
        });
        
        // Add click handler
        header.addEventListener('click', function() {
            sortTable(table, index, this);
        });
        
        // Add sorting indicator
        addSortIndicator(header);
    });
}

function sortTable(table, columnIndex, header) {
    const tbody = table.querySelector('tbody');
    const rows = Array.from(tbody.querySelectorAll('tr'));
    
    // Determine sort direction
    const currentSort = header.getAttribute('data-sort');
    const isAscending = currentSort !== 'asc';
    const newSort = isAscending ? 'asc' : 'desc';
    
    // Clear other headers' sort indicators
    const allHeaders = table.querySelectorAll('thead th');
    allHeaders.forEach(h => {
        h.removeAttribute('data-sort');
        h.classList.remove('sort-asc', 'sort-desc');
        const indicator = h.querySelector('.sort-indicator');
        if (indicator) {
            indicator.textContent = '↕';
        }
    });
    
    // Set current header sort state
    header.setAttribute('data-sort', newSort);
    header.classList.add(isAscending ? 'sort-asc' : 'sort-desc');
    
    // Update sort indicator
    const indicator = header.querySelector('.sort-indicator');
    if (indicator) {
        indicator.textContent = isAscending ? '▲' : '▼';
    }
    
    // Sort rows
    rows.sort((a, b) => {
        const aCell = a.cells[columnIndex];
        const bCell = b.cells[columnIndex];
        
        let aValue = aCell.textContent.trim();
        let bValue = bCell.textContent.trim();
        
        // Handle numeric values
        const aNum = parseFloat(aValue.replace(/[^\d.-]/g, ''));
        const bNum = parseFloat(bValue.replace(/[^\d.-]/g, ''));
        
        if (!isNaN(aNum) && !isNaN(bNum)) {
            aValue = aNum;
            bValue = bNum;
        }
        
        // Handle date values (basic detection)
        const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
        if (dateRegex.test(aValue) && dateRegex.test(bValue)) {
            aValue = new Date(aValue);
            bValue = new Date(bValue);
        }
        
        // Compare values
        let comparison = 0;
        if (aValue < bValue) {
            comparison = -1;
        } else if (aValue > bValue) {
            comparison = 1;
        }
        
        return isAscending ? comparison : -comparison;
    });
    
    // Re-append sorted rows
    rows.forEach(row => tbody.appendChild(row));
}

function addSortIndicator(header) {
    const indicator = document.createElement('span');
    indicator.className = 'sort-indicator';
    indicator.textContent = '↕';
    indicator.style.marginLeft = '5px';
    indicator.style.fontSize = '0.8em';
    indicator.style.color = '#6c757d';
    header.appendChild(indicator);
}

// Add CSS styles for sorting (dark theme compatible)
const style = document.createElement('style');
style.textContent = `
    .sortable-table th {
        position: relative;
        cursor: pointer;
        user-select: none;
        transition: all 0.2s ease;
    }
    
    .sortable-table th:hover {
        background-color: var(--dark-bg-hover) !important;
    }
    
    .sort-asc {
        background-color: var(--dark-bg-active) !important;
        color: white !important;
        font-weight: bold;
    }
    
    .sort-desc {
        background-color: var(--dark-info) !important;
        color: white !important;
        font-weight: bold;
    }
    
    .sort-indicator {
        transition: all 0.2s ease;
        color: var(--dark-text-accent) !important;
        margin-left: 5px;
        font-size: 0.8em;
    }
    
    .sortable-table th:hover .sort-indicator {
        color: white !important;
    }
`;
document.head.appendChild(style);
