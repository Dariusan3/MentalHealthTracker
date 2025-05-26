// Funcții pentru sincronizarea stării de autentificare între localStorage și sesiune

// Verificăm starea de autentificare la încărcarea paginii
document.addEventListener('DOMContentLoaded', function() {
    console.log('auth-sync.js: Verificare stare autentificare la încărcare');
    
    // Verificăm dacă am reîmprospătat deja autentificarea în această sesiune
    if (sessionStorage.getItem('auth_refreshed') === 'true') {
        console.log('auth-sync.js: Autentificarea a fost deja reîmprospătată în această sesiune');
        return;
    }
    
    // Verificăm dacă suntem pe o pagină de autentificare/înregistrare
    if (window.location.pathname.includes('/account/login') || 
        window.location.pathname.includes('/account/register')) {
        console.log('auth-sync.js: Pe pagina de autentificare/înregistrare, nu este necesară sincronizarea');
        return;
    }
    
    // Încercăm să sincronizăm autentificarea
    syncAuthStateFromLocalStorage();
});

// Funcția principală pentru sincronizarea autentificării
async function syncAuthStateFromLocalStorage() {
    try {
        // Verificăm dacă avem informații de autentificare în localStorage
        const isAuthenticated = localStorage.getItem('isAuthenticated') === 'true';
        const userId = localStorage.getItem('userId');
        const userName = localStorage.getItem('userName');
        
        console.log(`auth-sync.js: Stare localStorage - autentificat: ${isAuthenticated}, user: ${userName}`);
        
        if (isAuthenticated && userId) {
            console.log('auth-sync.js: Încercare sincronizare sesiune cu localStorage');
            
            // Verificăm starea autentificării pe server
            const statusResponse = await fetch('/api/account/status');
            const statusData = await statusResponse.json();
            
            // Dacă nu suntem autentificați pe server, dar avem date în localStorage
            if (!statusData.isAuthenticated) {
                console.log('auth-sync.js: Sesiune expirată, încercare reautentificare');
                
                // Setăm un flag pentru a evita bucle infinite
                if (sessionStorage.getItem('refresh_attempts') === null) {
                    sessionStorage.setItem('refresh_attempts', '0');
                }
                
                const attempts = parseInt(sessionStorage.getItem('refresh_attempts'));
                if (attempts >= 3) {
                    console.log('auth-sync.js: Prea multe încercări de reîmprospătare, ștergere date localStorage');
                    localStorage.removeItem('isAuthenticated');
                    localStorage.removeItem('userId');
                    localStorage.removeItem('userName');
                    sessionStorage.removeItem('refresh_attempts');
                    return;
                }
                
                sessionStorage.setItem('refresh_attempts', (attempts + 1).toString());
                
                // Încercăm să reîmprospătăm sesiunea
                const refreshResponse = await fetch('/api/account/refresh', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        userId: userId,
                        userName: userName
                    })
                });
                
                const refreshData = await refreshResponse.json();
                console.log('auth-sync.js: Răspuns reîmprospătare:', refreshData);
                
                if (refreshData.success) {
                    console.log('auth-sync.js: Reautentificare reușită, reîncărcare pagină');
                    
                    // Setăm un flag pentru a evita bucla infinită
                    sessionStorage.setItem('auth_refreshed', 'true');
                    sessionStorage.removeItem('refresh_attempts');
                    
                    // Reîncărcăm pagina pentru a actualiza starea UI
                    setTimeout(() => {
                        window.location.reload();
                    }, 500);
                } else {
                    console.log('auth-sync.js: Reautentificare eșuată, ștergere date localStorage');
                    
                    // Ștergem datele din localStorage dacă reîmprospătarea a eșuat
                    localStorage.removeItem('isAuthenticated');
                    localStorage.removeItem('userId');
                    localStorage.removeItem('userName');
                    sessionStorage.removeItem('refresh_attempts');
                }
            } else {
                console.log('auth-sync.js: Sesiune validă, nu este necesară reautentificarea');
                sessionStorage.removeItem('refresh_attempts');
            }
        } else {
            console.log('auth-sync.js: Nu există date de autentificare în localStorage');
        }
    } catch (error) {
        console.error('auth-sync.js: Eroare la sincronizarea autentificării:', error);
    }
}

// Exportăm funcția pentru a o putea apela din Blazor
window.syncAuthStateFromLocalStorage = syncAuthStateFromLocalStorage; 