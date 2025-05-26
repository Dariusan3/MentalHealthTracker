// Funcții pentru desenarea graficelor

// Definim namespace-ul chartUtils
window.chartUtils = {
    // Funcție pentru desenarea graficului de tendință a stării de spirit
    createMoodTrendChart: function(containerId, dates, moodValues) {
        console.log('Apel createMoodTrendChart cu:', { containerId, dates, moodValues });
        
        // Verificăm dacă avem datele necesare
        if (!dates || !moodValues || dates.length === 0 || moodValues.length === 0) {
            console.error('Date insuficiente pentru desenarea graficului de tendință');
            return;
        }
        
        // Verificăm dacă există elementul container
        const chartContainer = document.getElementById(containerId);
        console.log('Container găsit:', chartContainer);
        
        if (!chartContainer) {
            console.error(`Elementul container pentru grafic (${containerId}) nu a fost găsit`);
            
            // Verificăm toate elementele cu ID-uri din pagină pentru diagnosticare
            const allElementsWithId = document.querySelectorAll('[id]');
            console.log('Elemente cu ID-uri disponibile:', Array.from(allElementsWithId).map(el => el.id));
            
            // Verificăm dacă pagina este complet încărcată
            console.log('Document ready state:', document.readyState);
            
            // Încercăm din nou după un scurt delay
            setTimeout(() => {
                const retryContainer = document.getElementById(containerId);
                console.log('Reîncercare găsire container după delay:', retryContainer);
                
                if (retryContainer) {
                    console.log('Container găsit la a doua încercare, desenăm graficul');
                    this.createMoodTrendChartInternal(retryContainer, dates, moodValues);
                }
            }, 1000);
            
            return;
        }
        
        // Verificăm dacă Chart.js este disponibil
        if (typeof Chart === 'undefined') {
            // Încărcăm Chart.js dacă nu este disponibil
            console.log('Încărcare Chart.js...');
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
            script.onload = function() {
                console.log('Chart.js încărcat cu succes');
                // După încărcare, desenăm graficul
                chartUtils.createMoodTrendChartInternal(chartContainer, dates, moodValues);
            };
            document.head.appendChild(script);
        } else {
            console.log('Chart.js este deja disponibil, desenăm graficul');
            // Chart.js este deja încărcat, desenăm graficul
            this.createMoodTrendChartInternal(chartContainer, dates, moodValues);
        }
    },
    
    // Funcție internă pentru crearea graficului de tendință
    createMoodTrendChartInternal: function(container, dates, moodValues) {
        // Curățăm containerul în caz că există deja un grafic
        container.innerHTML = '';
        const canvas = document.createElement('canvas');
        container.appendChild(canvas);
        
        // Creăm contextul pentru grafic
        const ctx = canvas.getContext('2d');
        
        // Definim culorile pentru diferite niveluri de stare de spirit
        const getColorForMood = (value) => {
            if (value <= 3) return 'rgba(244, 67, 54, 0.7)'; // Roșu pentru stări negative
            if (value <= 5) return 'rgba(255, 152, 0, 0.7)'; // Portocaliu pentru stări neutre joase
            if (value <= 7) return 'rgba(3, 169, 244, 0.7)'; // Albastru pentru stări neutre înalte
            return 'rgba(76, 175, 80, 0.7)'; // Verde pentru stări pozitive
        };
        
        // Generăm culorile pentru fiecare punct de date
        const pointColors = moodValues.map(getColorForMood);
        
        // Creăm graficul
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: dates,
                datasets: [{
                    label: 'Nivel stare de spirit',
                    data: moodValues,
                    borderColor: 'rgba(75, 192, 192, 1)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.4,
                    pointBackgroundColor: pointColors,
                    pointBorderColor: pointColors,
                    pointRadius: 6,
                    pointHoverRadius: 8
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        min: 1,
                        max: 10,
                        ticks: {
                            stepSize: 1
                        },
                        title: {
                            display: true,
                            text: 'Nivel stare de spirit (1-10)'
                        }
                    },
                    x: {
                        title: {
                            display: true,
                            text: 'Data'
                        }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const value = context.parsed.y;
                                let label = 'Nivel: ' + value;
                                
                                // Adăugăm o descriere pentru nivel
                                if (value <= 3) label += ' (Negativ)';
                                else if (value <= 5) label += ' (Neutru jos)';
                                else if (value <= 7) label += ' (Neutru înalt)';
                                else label += ' (Pozitiv)';
                                
                                return label;
                            }
                        }
                    }
                }
            }
        });
    }
};

// Funcție pentru desenarea graficului de evoluție a stării de spirit
window.renderMoodChart = function(dates, moodValues) {
    // Verificăm dacă avem datele necesare
    if (!dates || !moodValues || dates.length === 0 || moodValues.length === 0) {
        console.error('Date insuficiente pentru desenarea graficului');
        return;
    }
    
    // Verificăm dacă există elementul container
    const chartContainer = document.getElementById('moodChart');
    if (!chartContainer) {
        console.error('Elementul container pentru grafic nu a fost găsit');
        return;
    }
    
    // Verificăm dacă Chart.js este disponibil
    if (typeof Chart === 'undefined') {
        // Încărcăm Chart.js dacă nu este disponibil
        console.log('Încărcare Chart.js...');
        const script = document.createElement('script');
        script.src = 'https://cdn.jsdelivr.net/npm/chart.js';
        script.onload = function() {
            // După încărcare, desenăm graficul
            createMoodChart(chartContainer, dates, moodValues);
        };
        document.head.appendChild(script);
    } else {
        // Chart.js este deja încărcat, desenăm graficul
        createMoodChart(chartContainer, dates, moodValues);
    }
};

// Funcție internă pentru crearea graficului
function createMoodChart(container, dates, moodValues) {
    // Curățăm containerul în caz că există deja un grafic
    container.innerHTML = '';
    const canvas = document.createElement('canvas');
    container.appendChild(canvas);
    
    // Creăm contextul pentru grafic
    const ctx = canvas.getContext('2d');
    
    // Definim culorile pentru diferite niveluri de stare de spirit
    const getColorForMood = (value) => {
        if (value <= 3) return 'rgba(244, 67, 54, 0.7)'; // Roșu pentru stări negative
        if (value <= 5) return 'rgba(255, 152, 0, 0.7)'; // Portocaliu pentru stări neutre joase
        if (value <= 7) return 'rgba(3, 169, 244, 0.7)'; // Albastru pentru stări neutre înalte
        return 'rgba(76, 175, 80, 0.7)'; // Verde pentru stări pozitive
    };
    
    // Generăm culorile pentru fiecare punct de date
    const pointColors = moodValues.map(getColorForMood);
    
    // Creăm graficul
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: dates,
            datasets: [{
                label: 'Nivel stare de spirit',
                data: moodValues,
                borderColor: 'rgba(75, 192, 192, 1)',
                backgroundColor: 'rgba(75, 192, 192, 0.2)',
                tension: 0.4,
                pointBackgroundColor: pointColors,
                pointBorderColor: pointColors,
                pointRadius: 6,
                pointHoverRadius: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    min: 1,
                    max: 10,
                    ticks: {
                        stepSize: 1
                    },
                    title: {
                        display: true,
                        text: 'Nivel stare de spirit (1-10)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Data'
                    }
                }
            },
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const value = context.parsed.y;
                            let label = 'Nivel: ' + value;
                            
                            // Adăugăm o descriere pentru nivel
                            if (value <= 3) label += ' (Negativ)';
                            else if (value <= 5) label += ' (Neutru jos)';
                            else if (value <= 7) label += ' (Neutru înalt)';
                            else label += ' (Pozitiv)';
                            
                            return label;
                        }
                    }
                }
            }
        }
    });
} 