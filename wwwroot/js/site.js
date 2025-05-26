// Funcție pentru redirecționare cu timeout
window.redirectWithTimeout = function (dotnetHelper, methodName, timeoutMs) {
    setTimeout(function () {
        dotnetHelper.invokeMethodAsync(methodName);
    }, timeoutMs);
};

// Funcție pentru redirecționare directă
window.redirectToUrl = function (url) {
    window.location.href = url;
};

// Funcție pentru forțarea deconectării complete
window.forceLogout = function() {
    console.log('Forțare deconectare completă...');
    
    // Ștergem toate datele de autentificare din localStorage
    localStorage.clear();
    
    // Ștergem toate flag-urile din sessionStorage
    sessionStorage.clear();
    
    // Ștergem cookie-urile de autentificare
    document.cookie.split(";").forEach(function(c) {
        document.cookie = c.trim().split('=')[0] + '=;' + 'expires=Thu, 01 Jan 1970 00:00:00 UTC;path=/;';
        console.log('Cookie șters:', c);
    });
    
    // Redirecționăm către endpoint-ul de deconectare directă
    window.location.href = '/api/account/logout-direct?t=' + new Date().getTime();
};

// Inițializăm obiectul authHelpers dacă nu există deja
if (!window.authHelpers) {
    window.authHelpers = {};
}

// Extindem obiectul authHelpers cu funcționalități de bază
Object.assign(window.authHelpers, {
    // Setăm un flag în sessionStorage pentru a indica o autentificare reușită
    setLoginSuccess: function () {
        console.log("Setare flag autentificare reușită");
        sessionStorage.setItem('login_success', 'true');
        return true;
    },

    // Verificăm dacă există flag-ul de autentificare reușită
    checkLoginSuccess: function () {
        const success = sessionStorage.getItem('login_success') === 'true';
        console.log("Verificare flag autentificare:", success);
        return success;
    },

    // Ștergem flag-ul de autentificare reușită
    clearLoginSuccess: function () {
        console.log("Ștergere flag autentificare");
        sessionStorage.removeItem('login_success');
    },

    // Forțăm reîncărcarea paginii
    forceReload: function () {
        console.log("Reîncărcare pagină");
        location.reload();
    },
    
    // Verificăm starea de autentificare prin API
    checkAuthStatus: function () {
        console.log("Verificare stare autentificare prin API");
        return fetch('/api/account/status')
            .then(response => response.json())
            .then(data => {
                console.log("Răspuns API autentificare:", data);
                
                // Dacă API-ul spune că suntem autentificați, actualizăm localStorage
                if (data.isAuthenticated) {
                    localStorage.setItem('isAuthenticated', 'true');
                    localStorage.setItem('userId', data.userId);
                    localStorage.setItem('userName', data.userName);
                } else {
                    // Verificăm dacă avem informații de autentificare în localStorage
                    const localAuth = localStorage.getItem('isAuthenticated') === 'true';
                    if (localAuth) {
                        console.log("Autentificare găsită în localStorage dar nu în API - posibilă pierdere de sesiune");
                    }
                }
                
                return data;
            })
            .catch(error => {
                console.error("Eroare la verificarea autentificării:", error);
                return { isAuthenticated: false, userName: "", userId: "" };
            });
    },
    
    // Afișăm informații de diagnosticare în consolă
    logAuthInfo: function () {
        console.log("Verificare cookies:");
        document.cookie.split(';').forEach(function(c) {
            console.log(c.trim());
        });
        
        // Verificăm și localStorage
        console.log("Verificare localStorage:");
        console.log("isAuthenticated:", localStorage.getItem('isAuthenticated'));
        console.log("userId:", localStorage.getItem('userId'));
        console.log("userName:", localStorage.getItem('userName'));
        
        this.checkAuthStatus().then(data => {
            console.log("Stare autentificare:", data);
        });
    },
    
    // Verificăm starea de autentificare din localStorage
    getLocalAuthState: function() {
        return {
            isAuthenticated: localStorage.getItem('isAuthenticated') === 'true',
            userId: localStorage.getItem('userId'),
            userName: localStorage.getItem('userName')
        };
    }
});

// Rulăm verificarea autentificării la încărcarea paginii
document.addEventListener('DOMContentLoaded', function () {
    console.log("DOM încărcat, verificare autentificare");
    window.authHelpers.logAuthInfo();
    
    // Verificăm dacă trebuie să reîncărcăm pagina după autentificare
    if (window.authHelpers.checkLoginSuccess()) {
        console.log("Flag autentificare detectat, reîncărcare pagină");
        window.authHelpers.clearLoginSuccess();
        window.authHelpers.forceReload();
    }
});

// Forțăm reîmprospătarea stării de autentificare
window.forceAuthRefresh = function() {
    console.log("Forțare reîmprospătare stare autentificare");
    
    // Verificăm dacă am încercat deja reîmprospătarea
    if (sessionStorage.getItem('tried_refresh') === 'true') {
        console.log("Am încercat deja reîmprospătarea, se ignoră");
        return Promise.resolve({ success: false, isAuthenticated: false });
    }
    
    // Setăm flag-ul pentru a evita bucla infinită
    sessionStorage.setItem('tried_refresh', 'true');
    
    return fetch('/api/account/refresh', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            userId: localStorage.getItem('userId'),
            userName: localStorage.getItem('userName')
        })
    })
    .then(response => response.json())
    .then(data => {
        console.log("Răspuns reîmprospătare:", data);
        if (data.isAuthenticated) {
            console.log("Autentificat ca:", data.userName);
            
            // Ștergem flag-ul după 5 secunde pentru a permite reîncercarea ulterioară
            setTimeout(function() {
                sessionStorage.removeItem('tried_refresh');
            }, 5000);
            
            // Reîncărcăm pagina pentru a actualiza starea
            location.reload();
        } else {
            console.log("Nu sunteți autentificat");
            
            // Curățăm localStorage dacă autentificarea a eșuat
            localStorage.removeItem('isAuthenticated');
            localStorage.removeItem('userId');
            localStorage.removeItem('userName');
            
            // Ștergem flag-ul
            sessionStorage.removeItem('tried_refresh');
        }
        return data;
    })
    .catch(error => {
        console.error("Eroare la reîmprospătarea autentificării:", error);
        
        // Ștergem flag-ul în caz de eroare
        sessionStorage.removeItem('tried_refresh');
        
        return { success: false, isAuthenticated: false };
    });
};

// Configurăm interceptorul pentru cereri HTTP
window.configureHttpInterceptor = function() {
    console.log("Configurare interceptor HTTP pentru a asigura transmiterea cookie-urilor");
    
    // Adăugăm un event listener pentru a intercepta toate cererile fetch
    const originalFetch = window.fetch;
    window.fetch = function(url, options) {
        // Asigurăm-ne că avem opțiuni
        options = options || {};
        
        // Asigurăm-ne că avem headers
        options.headers = options.headers || {};
        
        // Adăugăm credentials: 'include' pentru a trimite cookie-urile
        options.credentials = 'include';
        
        // Adăugăm un header pentru debugging
        options.headers['X-Requested-With'] = 'XMLHttpRequest';
        
        console.log(`Cerere interceptată către ${url}`);
        
        // Apelăm fetch-ul original
        return originalFetch(url, options);
    };
    
    console.log("Interceptor HTTP configurat cu succes");
}; 