Для отправки сообщения на почту нужно ввести в терминале visual studio эту команду - $body = '{"to":"ваша_почта","title":"Тестовое письмо","message":"<h1>Привет! Это тестовое сообщение</h1><p>Привет, мир!</p>"}'
Invoke-RestMethod -Uri "http://localhost:1337/auth/sendEmail" -Method POST -ContentType "application/json; charset=utf-8" -Body $body
