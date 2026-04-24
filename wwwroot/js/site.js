// Auto-dismiss toasts
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.toast').forEach(toast => {
        setTimeout(() => {
            toast.style.transition = 'opacity 0.4s';
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 400);
        }, 4000);
    });
});
