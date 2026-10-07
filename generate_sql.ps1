$tenantId = 'aaaaaaaa-0000-0000-0000-000000000001'
$uomId = 'bbbbbbbb-0000-0000-0000-000000000002'
$sqlFile = 'insert_productos.sql'
$out = 'INSERT INTO productos (id, tenant_id, nombre, descripcion, codigo_barras, uom_base_id, precio_base, es_peso_variable, is_active, created_at) VALUES ' + "
"

$prefixes = @("Super", "Maxi", "Eco", "Premium", "Vital", "Natural", "Sano", "Delicia", "Puro", "Clasico")
$items = @("Agua", "Jugo", "Galletas", "Pan", "Cafe", "Te", "Cereal", "Leche", "Yoghurt", "Queso", "Jamon", "Arroz", "Fideos", "Aceite", "Sal", "Azucar", "Mantequilla", "Manjar", "Mermelada", "Chocolate", "Caramelos", "Papas Fritas", "Cerveza", "Vino", "Atun", "Salsa de Tomate", "Mayonesa", "Ketchup", "Mostaza", "Vinagre")
$suffixes = @("1L", "2L", "500ml", "1kg", "500g", "250g", "Pack 6", "Pack 12", "Familiar", "Individual")

$values = @()
for ($i = 1; $i -le 300; $i++) {
    $id = [guid]::NewGuid().ToString()
    $name = "$($prefixes | Get-Random) $($items | Get-Random) $($suffixes | Get-Random)"
    $desc = "Producto de alta calidad $name"
    $barcode = (1000000000000 + $i).ToString()
    $price = (Get-Random -Minimum 5 -Maximum 100) * 100
    $values += "('$id', '$tenantId', '$name', '$desc', '$barcode', '$uomId', $price, false, true, NOW())"
}

$out += ($values -join ",
") + ";"
Set-Content -Path $sqlFile -Value $out -Encoding UTF8
