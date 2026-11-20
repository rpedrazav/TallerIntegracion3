#!/usr/bin/env bash
# S3-031: prueba manual de login y administración de usuarios vía Kong (:8000).
# Imprime solo códigos HTTP y campos no sensibles (sin tokens).
K=http://localhost:8000/api
T=aaaaaaaa-0000-0000-0000-000000000001
py() { python -c "import sys,json; d=json.load(sys.stdin); $1"; }
req() { # metodo url token body -> imprime "CODE" y deja el cuerpo en /tmp body file
  local m=$1 u=$2 tok=$3 b=$4
  curl -s -o "$BODY" -w "%{http_code}" -X "$m" "$u" -H "Content-Type: application/json" \
    ${tok:+-H "Authorization: Bearer $tok"} ${b:+-d "$b"}
}
BODY=$(mktemp)
login() { req POST "$K/auth/login" "" "{\"email\":\"$1\",\"password\":\"$2\",\"tenantId\":\"$T\"}"; }
claims() { cat "$BODY" | py "import base64;t=d['token'].split('.')[1];t+='='*(-len(t)%4);c=json.loads(base64.urlsafe_b64decode(t));print('   claims:',{k:c.get(k) for k in ['tenant_id','active_role','sucursal_id','iss','aud']},'| expiresAt:',d.get('expiresAt'),'| campos:',sorted(d.keys()))"; }

echo "1. Login admin@demo.cl -> $(login admin@demo.cl demo1234)"; claims
ADMIN=$(cat "$BODY" | py "print(d['token'])")
echo "2. Login cajero@demo.cl -> $(login cajero@demo.cl demo1234)"; claims
CAJERO=$(cat "$BODY" | py "print(d['token'])")
echo "3. Login password incorrecta -> $(login admin@demo.cl incorrecta) | cuerpo: $(cat "$BODY")"
echo "4. Login email inexistente -> $(login nadie@demo.cl demo1234) | cuerpo: $(cat "$BODY")"
echo "5. Login body inválido (email vacío) -> $(req POST "$K/auth/login" "" "{\"email\":\"\",\"password\":\"\",\"tenantId\":\"$T\"}")"
echo "6. GET /users (admin) -> $(req GET "$K/users" "$ADMIN")"; cat "$BODY" | py "print('   usuarios:',len(d if isinstance(d,list) else d.get('items',[])))"
echo "7. GET /users/roles (admin) -> $(req GET "$K/users/roles" "$ADMIN")"; cat "$BODY" | py "print('   roles:',[r.get('nombre') or r.get('name') for r in d])"
echo "8. GET /sucursales (admin) -> $(req GET "$K/sucursales" "$ADMIN")"; cat "$BODY" | py "print('   sucursales:',len(d))"
echo "9. GET /users (cajero) -> $(req GET "$K/users" "$CAJERO")"
echo "10. GET /users sin token -> $(req GET "$K/users" "")"
EMAIL="s3031.$(date +%s)@demo.cl"
echo "11. POST /users ($EMAIL, rol CAJERO) -> $(req POST "$K/users" "$ADMIN" "{\"nombre\":\"Regresion de prueba\",\"email\":\"$EMAIL\",\"password\":\"Prueba1234!\",\"roles\":[\"CAJERO\"]}")"
NEWID=$(cat "$BODY" | py "print(d.get('id',''))" 2>/dev/null); cat "$BODY" | py "print('   roles:',d.get('roles'),'| tenantId propio:',str(d.get('tenantId'))=='$T')" 2>/dev/null || cat "$BODY"
echo "12. Login usuario nuevo -> $(login "$EMAIL" Prueba1234!)"; claims
echo "13. POST /users email duplicado -> $(req POST "$K/users" "$ADMIN" "{\"nombre\":\"Dup\",\"email\":\"$EMAIL\",\"password\":\"Prueba1234!\",\"roles\":[\"CAJERO\"]}")"
echo "14. PUT /users/{id} nombre -> $(req PUT "$K/users/$NEWID" "$ADMIN" "{\"nombre\":\"Regresion editado\",\"email\":\"$EMAIL\"}") | $(cat "$BODY" | head -c 200)"
echo "15. DELETE /users/{id} (desactivar) -> $(req DELETE "$K/users/$NEWID" "$ADMIN")"
echo "16. Login usuario desactivado -> $(login "$EMAIL" Prueba1234!) | cuerpo: $(cat "$BODY")"
rm -f "$BODY"
