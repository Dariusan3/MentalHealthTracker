// Funcții pentru gestionarea autentificării

// Verificăm dacă obiectul authHelpers există deja
if (!window.authHelpers) {
    window.authHelpers = {};
}

// Extindem obiectul authHelpers cu funcționalitățile de autentificare
Object.assign(window.authHelpers, {
    // Salvează starea de autentificare în localStorage
    saveAuthState: function (isAuthenticated, userId, userName) {
        localStorage.setItem('isAuthenticated', isAuthenticated);
        localStorage.setItem('userId', userId);
        localStorage.setItem('userName', userName);
        console.log(`Stare autentificare salvată: ${userName}`);
    },

    // Obține starea de autentificare din localStorage
    getLocalAuthState: function () {
        return {
            IsAuthenticated: localStorage.getItem('isAuthenticated') === 'true',
            UserId: localStorage.getItem('userId') || '',
            UserName: localStorage.getItem('userName') || ''
        };
    },

    // Șterge starea de autentificare din localStorage
    clearAuthState: function () {
        localStorage.removeItem('isAuthenticated');
        localStorage.removeItem('userId');
        localStorage.removeItem('userName');
        
        // Curățăm și flag-urile din sessionStorage
        sessionStorage.removeItem('login_success');
        sessionStorage.removeItem('tried_refresh');
        sessionStorage.removeItem('tried_reload');
        sessionStorage.removeItem('reload_attempts');
        
        console.log('Stare autentificare ștearsă');
    },

    // Verifică dacă avem un flag de autentificare reușită
    checkLoginSuccess: function () {
        return sessionStorage.getItem('login_success') === 'true';
    },

    // Setează flag-ul de autentificare reușită
    setLoginSuccess: function () {
        sessionStorage.setItem('login_success', 'true');
    },

    // Șterge flag-ul de autentificare reușită
    clearLoginSuccess: function () {
        sessionStorage.removeItem('login_success');
    }
});

// Funcție pentru verificarea stării de autentificare
window.checkAuthStatus = async function () {
    try {
        const response = await fetch('/api/account/status');
        const data = await response.json();
        
        console.log('Stare autentificare API:', data);
        
        if (data.isAuthenticated) {
            // Salvăm starea în localStorage
            window.authHelpers.saveAuthState(true, data.userId, data.userName);
        }
        
        return data;
    } catch (error) {
        console.error('Eroare la verificarea stării de autentificare:', error);
        return { isAuthenticated: false };
    }
};

// Funcție pentru reîmprospătarea forțată a autentificării
window.forceAuthRefresh = async function () {
    try {
        // Verificăm dacă avem deja prea multe încercări
        const reloadAttempts = parseInt(sessionStorage.getItem('reload_attempts') || '0');
        if (reloadAttempts >= 5) {
            console.log('Prea multe încercări de reîncărcare, se oprește procesul');
            sessionStorage.removeItem('reload_attempts');
            return;
        }
        
        // Incrementăm contorul de încercări
        sessionStorage.setItem('reload_attempts', (reloadAttempts + 1).toString());
        
        // Obținem starea din localStorage
        const localState = window.authHelpers.getLocalAuthState();
        
        if (localState.IsAuthenticated && localState.UserId) {
            console.log(`Încercare de reîmprospătare sesiune pentru ${localState.UserName}`);
            
            // Apelăm API-ul pentru reîmprospătarea sesiunii
            const response = await fetch('/api/account/refresh', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    userId: localState.UserId,
                    userName: localState.UserName
                })
            });
            
            const data = await response.json();
            console.log('Răspuns reîmprospătare:', data);
            
            if (data.success) {
                // Reîncărcăm pagina pentru a actualiza starea de autentificare
                window.location.reload();
            }
        }
    } catch (error) {
        console.error('Eroare la reîmprospătarea autentificării:', error);
    }
};

// Funcție pentru deconectare
window.logout = async function () {
    try {
        console.log('Începere proces de deconectare...');
        
        // Ștergem starea din localStorage direct
        localStorage.removeItem('isAuthenticated');
        localStorage.removeItem('userId');
        localStorage.removeItem('userName');
        
        // Curățăm flag-urile din sessionStorage
        sessionStorage.removeItem('login_success');
        sessionStorage.removeItem('tried_refresh');
        sessionStorage.removeItem('tried_reload');
        sessionStorage.removeItem('reload_attempts');
        
        console.log('Stare autentificare ștearsă din localStorage');
        
        // Apelăm API-ul de deconectare
        const response = await fetch('/api/account/logout', { 
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        
        console.log('Răspuns deconectare:', response.status);
        
        // Curățăm și cookie-urile dacă este posibil
        document.cookie.split(";").forEach(function(c) {
            if (c.trim().startsWith('MentalHealthTracker.Auth')) {
                document.cookie = c.replace(/^ +/, "").replace(/=.*/, "=;expires=" + new Date().toUTCString() + ";path=/");
                console.log('Cookie șters:', c);
            }
        });
        
        // Forțăm reîncărcarea paginii după o scurtă pauză
        setTimeout(function() {
            console.log('Reîncărcare pagină după deconectare...');
            window.location.href = '/';
        }, 500);
    } catch (error) {
        console.error('Eroare la deconectare:', error);
        
        // Curățăm starea din localStorage oricum
        localStorage.removeItem('isAuthenticated');
        localStorage.removeItem('userId');
        localStorage.removeItem('userName');
        
        // Reîncărcăm pagina oricum
        window.location.href = '/';
    }
}; 