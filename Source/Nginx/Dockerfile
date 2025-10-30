# Используем официальный образ nginx
FROM nginx:alpine

# Удаляем дефолтную конфигурацию nginx
RUN rm /etc/nginx/conf.d/default.conf

# Копируем нашу конфигурацию
COPY nginx.conf /etc/nginx/nginx.conf

# Создаем папку для статических файлов фронтенда
RUN mkdir -p /usr/share/nginx/html

# Опционально: копируем SSL сертификаты (если используете HTTPS)
# COPY ssl/ /etc/nginx/ssl/

# Открываем порты
EXPOSE 80
EXPOSE 443

# Запускаем nginx
CMD ["nginx", "-g", "daemon off;"]