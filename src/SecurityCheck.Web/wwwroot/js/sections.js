document.addEventListener('click', event => {
    const link = event.target.closest('a[href^="#"]');

    if(!link) {
        return;
    }

    const target = document.getElementById(link.getAttribute('href').slice(1)); 
        if(target instanceof HTMLDetailsElement) {
            target.open=true;
    }
});