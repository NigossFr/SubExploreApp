# Test de connectivite reseau simple
Write-Host "Test de connectivite reseau..." -ForegroundColor Green

# Test DNS
try {
    $hostname = "iguvwnyehojvxkyqzaoi.supabase.co"
    $result = Resolve-DnsName -Name $hostname -ErrorAction Stop
    Write-Host "DNS OK: $hostname resolu" -ForegroundColor Green
} catch {
    Write-Host "DNS ECHEC: Impossible de resoudre $hostname" -ForegroundColor Red
}

# Test IP directe
try {
    $ip = "104.18.38.10"
    $result = Test-NetConnection -ComputerName $ip -Port 443 -InformationLevel Quiet
    if ($result) {
        Write-Host "IP OK: $ip accessible" -ForegroundColor Green
    } else {
        Write-Host "IP ECHEC: $ip non accessible" -ForegroundColor Red
    }
} catch {
    Write-Host "IP ECHEC: Erreur lors du test" -ForegroundColor Red
}

Write-Host "Test termine" -ForegroundColor Green