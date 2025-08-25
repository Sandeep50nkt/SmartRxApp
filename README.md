# SmartRx (.NET 9)

## Run order
Open three terminals (or configure multiple startup projects in VS Code):

```bash
cd SmartRx.AuthApi && dotnet run     # http://localhost:5001
cd SmartRx.DrugsApi && dotnet run    # http://localhost:5002
cd SmartRx.Web && dotnet run         # http://localhost:5000
```

## Swagger
```bash
http://localhost:5001/swagger
http://localhost:5002/swagger
```

## First time
Register an admin via Auth API:
```bash
curl -X POST http://localhost:5001/api/auth/register -H "Content-Type: application/json" -d '{"username":"admin","password":"admin123","role":"Admin"}'
```

Then browse: http://localhost:5000 and log in with `admin/admin123`.
