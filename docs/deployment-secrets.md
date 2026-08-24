# Deployment Secrets & Environment Variables

This document lists all environment variables required for deployment.

## Database

| Variable | Description | Format | Required |
|----------|-------------|--------|----------|
| `DB_CONNECTION` | MySQL connection string | `server=HOST;port=PORT;database=DB;user=USER;password=PASS` | Yes |

Example:
```
DB_CONNECTION=server=192.168.1.50;port=3306;database=OrderTrackingApp;user=prjt-ota;password=YOUR_SECURE_PASSWORD
```

## Email/SMTP

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `EMAIL_FROM` | Sender email address | `noreply@yourdomain.ch` | Yes |
| `SMTP_SERVER` | SMTP hostname | `mail.yourdomain.ch` | Yes |
| `SMTP_PORT` | SMTP port (usually 465 or 587) | `465` | Yes |
| `EMAIL_USERNAME` | SMTP authentication username | | Yes |
| `EMAIL_PASSWORD` | SMTP authentication password | | Yes |

## Project Storage

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `PROJECT_STORAGE_ROOT` | Base path for project files | `/mnt/archivio-progetti` | Yes (appsettings has default) |

## Security (Optional)

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `JWT_SECRET_KEY` | Secret key for JWT tokens (if using) | Auto-generated | No |
| `ENCRYPTION_KEY` | Data encryption key | Auto-generated | No |

---

## Setting Environment Variables

### Linux (systemd)

Add to `/etc/environment` or create `/etc/systemd/system/OrderTrackingApp.service.d/override.conf`:

```ini
[Service]
Environment=DB_CONNECTION=server=192.168.1.50;port=3306;database=OrderTrackingApp;user=prjt-ota;password=YOUR_PASSWORD_HERE
Environment=EMAIL_FROM=noreply@yourdomain.ch
Environment=SMTP_SERVER=mail.yourdomain.ch
Environment=SMTP_PORT=465
Environment=EMAIL_USERNAME=noreply@yourdomain.ch
Environment=EMAIL_PASSWORD=YOUR_EMAIL_PASSWORD_HERE
```

### Docker

```bash
docker run -d \
  -e DB_CONNECTION="server=..." \
  -e EMAIL_FROM="noreply@yourdomain.ch" \
  -e SMTP_SERVER="mail.yourdomain.ch" \
  -e SMTP_PORT="465" \
  -e EMAIL_USERNAME="noreply@yourdomain.ch" \
  -e EMAIL_PASSWORD="YOUR_PASSWORD_HERE" \
  ordertrackingapp:latest
```

Or use a `.env` file:

```bash
# .env file (add to .gitignore)
DB_CONNECTION=server=192.168.1.50;port=3306;database=OrderTrackingApp;user=prjt-ota;password=YOUR_PASSWORD_HERE
EMAIL_FROM=noreply@yourdomain.ch
SMTP_SERVER=mail.yourdomain.ch
SMTP_PORT=465
EMAIL_USERNAME=noreply@yourdomain.ch
EMAIL_PASSWORD=YOUR_EMAIL_PASSWORD_HERE
```

### Azure App Service

Configuration → Application settings → Add new settings.

### Kubernetes

Create a Secret:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: ordertrackingapp-secrets
type: Opaque
stringData:
  DB_CONNECTION: server=192.168.1.50;port=3306;database=OrderTrackingApp;user=prjt-ota;password=YOUR_PASSWORD_HERE
  EMAIL_FROM: noreply@yourdomain.ch
  SMTP_SERVER: mail.yourdomain.ch
  SMTP_PORT: "465"
  EMAIL_USERNAME: noreply@yourdomain.ch
  EMAIL_PASSWORD: YOUR_EMAIL_PASSWORD_HERE
```

Then reference in deployment.

---

## Required Variables Checklist

- [ ] `DB_CONNECTION`
- [ ] `EMAIL_FROM`
- [ ] `SMTP_SERVER`
- [ ] `SMTP_PORT`
- [ ] `EMAIL_USERNAME`
- [ ] `EMAIL_PASSWORD`

---

## Security Notes

1. **Never commit secrets to version control**
2. Use a secrets manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault) in production
3. Rotate passwords regularly
4. Use strong, unique passwords for each service
5. Enable SMTP SSL/TLS (port 465 uses SSL, port 587 uses STARTTLS)