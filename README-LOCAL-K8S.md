# Running the Microservices Locally — Docker Compose & Kubernetes

This guide covers the full workflow for running the four microservices locally,
from building Docker images all the way to accessing the Swagger UI in your browser.

## Where to run what

Two terminals are used throughout this guide:

| Terminal | Used for |
|---|---|
| **Devcontainer** (VS Code integrated terminal) | Building Docker images (`docker-compose`) |
| **Windows PowerShell** | Kubernetes commands (`kubectl`, `nerdctl`) |

`kubectl` and `nerdctl` are installed on Windows by Rancher Desktop and use the native
kubeconfig at `%USERPROFILE%\.kube\config` — no extra setup needed.

---

## Services and Ports

| Service           | Port |
|-------------------|------|
| InventoryService  | 5001 |
| OrderService      | 5002 |
| PaymentService    | 5003 |
| ProductService    | 5004 |

---

## Prerequisites

### Enable Kubernetes in Rancher Desktop

Open Rancher Desktop on Windows, go to **Preferences → Kubernetes** and make sure
**Enable Kubernetes** is checked. Apply and wait for the cluster to become ready
(the status indicator in the bottom-left turns green).

Rancher Desktop installs `kubectl` and `nerdctl` on Windows automatically and keeps
`%USERPROFILE%\.kube\config` up to date.

Verify from PowerShell:

```powershell
kubectl get nodes
```

---

## Step 1 — Build the Docker Images

> Run in the **devcontainer** terminal.

From the workspace root:

```bash
docker-compose build
```

Verify the images were created:

```bash
docker images | grep -E "order|product|inventory|payment"
```

You should see four images: `workspace-order-service`, `workspace-product-service`,
`workspace-inventory-service`, `workspace-payment-service`.

---

## Step 2 — Import Images into Kubernetes

> Run in **Windows PowerShell**.

The k8s deployments use `imagePullPolicy: Never`, meaning Kubernetes will never pull
from a registry — the image must already exist in the cluster's container runtime.

Rancher Desktop runs k3s on containerd internally, which is separate from the Docker
daemon used to build the images. Each image must be imported into the `k8s.io`
containerd namespace:

```powershell
docker save workspace-order-service     | nerdctl -n k8s.io load
docker save workspace-product-service   | nerdctl -n k8s.io load
docker save workspace-inventory-service | nerdctl -n k8s.io load
docker save workspace-payment-service   | nerdctl -n k8s.io load
```

Verify the images are visible to the cluster:

```powershell
nerdctl -n k8s.io images | Select-String -Pattern "order|product|inventory|payment"
```

> **Note:** This step must be repeated after every `docker-compose build` — rebuilt
> images are not automatically synced to the k8s runtime.

---

## Step 3 — Deploy to Kubernetes

> Run in **Windows PowerShell**.

From the repo root, apply the namespace and all manifests:

```powershell
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/
```

The second command applies all YAML files in `k8s/` (namespace, deployments, services).

---

## Step 4 — Verify Everything Is Running

> Run in **Windows PowerShell**.

### Check pods

```powershell
kubectl get pods -n microservices
```

Expected output (all pods `Running`, `READY 1/1`):

```
NAME                                  READY   STATUS    RESTARTS   AGE
inventory-service-xxxxxxxxx-xxxxx     1/1     Running   0          30s
order-service-xxxxxxxxx-xxxxx         1/1     Running   0          30s
payment-service-xxxxxxxxx-xxxxx       1/1     Running   0          30s
product-service-xxxxxxxxx-xxxxx       1/1     Running   0          30s
```

If a pod is in `ImagePullBackOff` or `ErrImageNeverPull`, the image was not imported —
go back to Step 2.

If a pod is in `CrashLoopBackOff`, check its logs:

```powershell
kubectl logs -n microservices deployment/product-service
```

### Check services

```powershell
kubectl get svc -n microservices
```

All four services should be listed with type `NodePort`.

---

## Step 5 — Access the Services

> Run in **Windows PowerShell**.

Use `kubectl port-forward` to expose each service on localhost. Run each command
in a separate PowerShell window (port-forward stays running):

```powershell
kubectl port-forward svc/product-service   5004:5004 -n microservices
kubectl port-forward svc/order-service     5002:5002 -n microservices
kubectl port-forward svc/inventory-service 5001:5001 -n microservices
kubectl port-forward svc/payment-service   5003:5003 -n microservices
```

Then open in your browser:

```
http://localhost:5004/swagger   ← ProductService
http://localhost:5002/swagger   ← OrderService
http://localhost:5001/swagger   ← InventoryService
http://localhost:5003/swagger   ← PaymentService
```

### If `localhost` does not work in the browser

`kubectl port-forward` binds to `127.0.0.1` (IPv4) by default. On some Windows
machines, `localhost` resolves to `::1` (IPv6) instead, so the browser never reaches
the port-forward listener.

**Fix:** use the explicit IPv4 address instead of `localhost`:

```
http://127.0.0.1:5004/swagger
http://127.0.0.1:5002/swagger
http://127.0.0.1:5001/swagger
http://127.0.0.1:5003/swagger
```

To confirm this is the issue, run in PowerShell:

```powershell
Resolve-DnsName localhost
```

If the result shows `::1` (IPv6) before `127.0.0.1`, that is the cause.
Using `127.0.0.1` directly bypasses the DNS resolution and always works.

---

## Quick Reference — Docker Compose (no Kubernetes)

For quick local testing without Kubernetes, run the services directly via Docker Compose
from the **devcontainer** terminal:

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f product-service

# Stop
docker-compose down
```

Services are directly accessible in your browser at `http://localhost:<port>/swagger`
without any port-forwarding.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `kubectl` not found in PowerShell | Rancher Desktop not installed or PATH not updated | Restart PowerShell after installing Rancher Desktop |
| `Unable to connect to the server` | Kubernetes not enabled in Rancher Desktop | Enable it under Preferences → Kubernetes |
| Pod stuck in `ImagePullBackOff` | Image not imported into containerd | Repeat Step 2 for the affected service |
| Pod stuck in `CrashLoopBackOff` | App startup failure | Run `kubectl logs -n microservices deployment/<service>` |
| Browser shows nothing on `localhost` | Port-forward not running | Start `kubectl port-forward` in PowerShell first |
