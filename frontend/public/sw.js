self.addEventListener('push', function (event) {
  if (!event.data) return;

  try {
    const payload = event.data.json();
    const options = {
      body: payload.body || 'Your daily engineering session is ready.',
      icon: payload.icon || '/favicon.svg',
      badge: payload.badge || '/favicon.svg',
      data: {
        url: payload.url || '/today'
      },
      tag: payload.tag || 'techdaily-daily',
      renotify: true,
      requireInteraction: false
    };

    event.waitUntil(
      self.registration.showNotification(payload.title || 'TechDaily', options)
    );
  } catch (err) {
    console.error('Error handling push event in Service Worker:', err);
  }
});

self.addEventListener('notificationclick', function (event) {
  event.notification.close();
  const targetUrl = (event.notification.data && event.notification.data.url) || '/today';

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function (clientList) {
      for (const client of clientList) {
        if (client.url.includes(self.registration.scope) && 'focus' in client) {
          client.navigate(targetUrl);
          return client.focus();
        }
      }
      if (clients.openWindow) {
        return clients.openWindow(targetUrl);
      }
    })
  );
});
