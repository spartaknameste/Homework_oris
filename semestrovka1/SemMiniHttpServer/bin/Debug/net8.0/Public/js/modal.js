document.addEventListener('DOMContentLoaded', function () {
    console.log('modal.js загружен');

    // Элементы модальных окон
    const loginButton = document.getElementById('loginButton');
    const modalOverlay = document.getElementById('modalOverlay');
    const modalForgotPassword = document.getElementById('modalForgotPassword');
    const modalRegister = document.getElementById('modalRegister');

    // Кнопки закрытия
    const modalClose = document.querySelector('.modal-close');
    const modalCloseForgot = document.querySelector('.modal-close-forgot');
    const modalCloseRegister = document.querySelector('.modal-close-register');

    // Переключение пароля
    const passwordToggle = document.querySelector('.password-toggle');
    const passwordInput = document.getElementById('passwordInput');
    const passwordToggleRegister = document.querySelector('.password-toggle-register');
    const registerPasswordInput = document.getElementById('registerPasswordInput');

    // Ссылки и кнопки
    const forgotPasswordLink = document.querySelector('.forgot-password');
    const btnRegister = document.querySelector('.btn-register');
    const btnBackLogin = document.querySelector('.btn-back-login');
    const btnBackLoginReg = document.querySelector('.btn-back-login-reg');

    // Формы
    const loginForm = document.querySelector('#modalOverlay .modal-form');
    const forgotPasswordForm = document.getElementById('forgotPasswordForm');
    const registerForm = document.getElementById('registerForm');

    // ========== ОТКРЫТИЕ/ЗАКРЫТИЕ МОДАЛЬНЫХ ОКОН ==========

    if (loginButton) {
        loginButton.addEventListener('click', function () {
            console.log('Открытие окна входа');
            modalOverlay.classList.add('active');
        });
    }

    if (modalClose) {
        modalClose.addEventListener('click', function () {
            modalOverlay.classList.remove('active');
        });
    }

    if (modalCloseForgot) {
        modalCloseForgot.addEventListener('click', function () {
            modalForgotPassword.classList.remove('active');
        });
    }

    if (modalCloseRegister) {
        modalCloseRegister.addEventListener('click', function () {
            modalRegister.classList.remove('active');
        });
    }

    if (modalOverlay) {
        modalOverlay.addEventListener('click', function (e) {
            if (e.target === modalOverlay) {
                modalOverlay.classList.remove('active');
            }
        });
    }

    if (modalForgotPassword) {
        modalForgotPassword.addEventListener('click', function (e) {
            if (e.target === modalForgotPassword) {
                modalForgotPassword.classList.remove('active');
            }
        });
    }

    if (modalRegister) {
        modalRegister.addEventListener('click', function (e) {
            if (e.target === modalRegister) {
                modalRegister.classList.remove('active');
            }
        });
    }

    // ========== ПЕРЕКЛЮЧЕНИЕ МЕЖДУ ОКНАМИ ==========

    if (forgotPasswordLink) {
        forgotPasswordLink.addEventListener('click', function (e) {
            e.preventDefault();
            modalOverlay.classList.remove('active');
            modalForgotPassword.classList.add('active');
        });
    }

    if (btnBackLogin) {
        btnBackLogin.addEventListener('click', function () {
            modalForgotPassword.classList.remove('active');
            modalOverlay.classList.add('active');
        });
    }

    if (btnRegister) {
        btnRegister.addEventListener('click', function () {
            console.log('Открытие окна регистрации');
            modalOverlay.classList.remove('active');
            modalRegister.classList.add('active');
        });
    }

    if (btnBackLoginReg) {
        btnBackLoginReg.addEventListener('click', function () {
            modalRegister.classList.remove('active');
            modalOverlay.classList.add('active');
        });
    }

    // ========== ПОКАЗАТЬ/СКРЫТЬ ПАРОЛЬ ==========

    if (passwordToggle && passwordInput) {
        passwordToggle.addEventListener('click', function () {
            passwordInput.type = passwordInput.type === 'password' ? 'text' : 'password';
        });
    }

    if (passwordToggleRegister && registerPasswordInput) {
        passwordToggleRegister.addEventListener('click', function () {
            registerPasswordInput.type = registerPasswordInput.type === 'password' ? 'text' : 'password';
        });
    }

    // ========== ОБРАБОТКА ФОРМЫ ВХОДА ==========

    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            console.log('📤 Отправка формы входа');

            const email = loginForm.querySelector('input[type="email"]').value;
            const password = loginForm.querySelector('input[type="password"]').value;

            console.log('Email:', email);

            const formData = new URLSearchParams();
            formData.append('email', email);
            formData.append('password', password);

            try {
                console.log('Отправка запроса на /auth/login');
                const response = await fetch('/auth/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: formData.toString()
                });

                console.log('Статус ответа:', response.status);
                const result = await response.json();
                console.log('Результат:', result);

                if (result.success) {
                    alert('✅ ' + result.message);
                    modalOverlay.classList.remove('active');
                    loginForm.reset();
                } else {
                    alert('❌ ' + result.error);
                }
            } catch (error) {
                console.error('Login error:', error);
                alert('Ошибка подключения к серверу');
            }
        });
    }

    // ========== ОБРАБОТКА ФОРМЫ РЕГИСТРАЦИИ ==========

    if (registerForm) {
        registerForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            console.log('Отправка формы регистрации');

            const formData = new FormData(registerForm);
            const data = new URLSearchParams(formData);

            console.log('Данные регистрации:', {
                username: formData.get('username'),
                email: formData.get('email')
            });

            const messageDiv = document.getElementById('registerMessage');

            try {
                console.log('Отправка запроса на /auth/register');
                const response = await fetch('/auth/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: data.toString()
                });

                console.log('Статус ответа:', response.status);
                const result = await response.json();
                console.log('Результат:', result);

                if (result.success) {
                    messageDiv.innerHTML = '<p style="color: #4caf50; font-weight: 500;">' + result.message + '</p>';
                    registerForm.reset();

                    setTimeout(() => {
                        modalRegister.classList.remove('active');
                        modalOverlay.classList.add('active');
                        messageDiv.innerHTML = '';
                    }, 3000);
                } else {
                    messageDiv.innerHTML = '<p style="color: #f44336; font-weight: 500;">❌ ' + result.error + '</p>';
                }
            } catch (error) {
                console.error('❌ Register error:', error);
                messageDiv.innerHTML = '<p style="color: #f44336;">Ошибка подключения к серверу</p>';
            }
        });
    }

    // ========== ОБРАБОТКА ФОРМЫ ВОССТАНОВЛЕНИЯ ПАРОЛЯ ==========

    if (forgotPasswordForm) {
        forgotPasswordForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            console.log('📤 Отправка формы восстановления пароля');

            const formData = new FormData(forgotPasswordForm);
            const data = new URLSearchParams(formData);

            const messageDiv = document.getElementById('forgotMessage');

            try {
                console.log('Отправка запроса на /auth/forgot-password');
                const response = await fetch('/auth/forgot-password', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: data.toString()
                });

                console.log('Статус ответа:', response.status);
                const result = await response.json();
                console.log('Результат:', result);

                if (result.success) {
                    messageDiv.innerHTML = '<p style="color: #4caf50; font-weight: 500;"> ' + result.message + '</p>';
                    forgotPasswordForm.reset();
                } else {
                    messageDiv.innerHTML = '<p style="color: #f44336; font-weight: 500;"> ' + result.error + '</p>';
                }
            } catch (error) {
                console.error('❌ Forgot password error:', error);
                messageDiv.innerHTML = '<p style="color: #f44336;">❌ Ошибка подключения к серверу</p>';
            }
        });
    }

    console.log('Все обработчики modal.js инициализированы');
});
