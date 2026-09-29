#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
validate.py - Validador del grafo de conocimiento GlobalMart OS
Uso: python docs/context/_tools/validate.py
Salida: 0 si todo OK, 1 si hay errores
"""
import io
import sys
# Force UTF-8 output on Windows
if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

import os
import re
import sys
from pathlib import Path

# Raíz del repositorio (2 niveles arriba desde _tools/)
REPO_ROOT = Path(__file__).parent.parent.parent.parent
CONTEXT_ROOT = REPO_ROOT / "docs" / "context"

errors = []
warnings = []
stats = {
    "total_nodes": 0,
    "nodes_with_frontmatter": 0,
    "nodes_missing_id": 0,
    "nodes_missing_estado": 0,
    "nodes_missing_fuentes": 0,
    "broken_links": [],
    "empty_files": 0,
}

# Todos los IDs de nodos válidos (para verificar enlaces)
VALID_IDS = set()


def parse_frontmatter(content: str) -> dict:
    """Extrae el frontmatter YAML de un archivo markdown."""
    if not content.startswith("---"):
        return {}
    end = content.find("---", 3)
    if end == -1:
        return {}
    fm_text = content[3:end]
    result = {}
    for line in fm_text.splitlines():
        if ":" in line:
            key, _, value = line.partition(":")
            result[key.strip()] = value.strip()
    return result


def collect_node_ids():
    """Recolecta todos los IDs definidos en los nodos."""
    for md_file in CONTEXT_ROOT.rglob("*.md"):
        if "_reports" in str(md_file) or "_tools" in str(md_file):
            continue
        content = md_file.read_text(encoding="utf-8", errors="replace")
        fm = parse_frontmatter(content)
        if "id" in fm:
            VALID_IDS.add(fm["id"].strip())
        # También agregamos el nombre del archivo sin extensión
        VALID_IDS.add(md_file.stem)


def validate_node(md_file: Path):
    """Valida un nodo markdown individual."""
    stats["total_nodes"] += 1

    content = md_file.read_text(encoding="utf-8", errors="replace")

    if not content.strip():
        stats["empty_files"] += 1
        errors.append(f"VACÍO: {md_file.relative_to(REPO_ROOT)}")
        return

    # Verifica frontmatter
    has_fm = content.startswith("---")
    if has_fm:
        stats["nodes_with_frontmatter"] += 1
        fm = parse_frontmatter(content)

        if "id" not in fm or not fm.get("id"):
            stats["nodes_missing_id"] += 1
            warnings.append(f"SIN id: {md_file.relative_to(REPO_ROOT)}")

        if "estado" not in fm or not fm.get("estado"):
            stats["nodes_missing_estado"] += 1
            warnings.append(f"SIN estado: {md_file.relative_to(REPO_ROOT)}")

        if "fuentes" not in fm or not fm.get("fuentes"):
            stats["nodes_missing_fuentes"] += 1
            warnings.append(f"SIN fuentes: {md_file.relative_to(REPO_ROOT)}")

        # Verifica que estado sea uno de los valores permitidos
        estado_value = fm.get("estado", "").strip("\"'[]")
        valid_estados = {
            "implementado", "parcial", "planificado", "obsoleto",
            "no_verificado"
        }
        if estado_value and estado_value.lower() not in valid_estados:
            warnings.append(
                f"estado inválido '{estado_value}': {md_file.relative_to(REPO_ROOT)}"
            )
    else:
        warnings.append(f"SIN frontmatter: {md_file.relative_to(REPO_ROOT)}")

    # Verifica que el archivo tenga al menos un encabezado H1
    if not re.search(r"^# .+", content, re.MULTILINE):
        warnings.append(f"SIN encabezado H1: {md_file.relative_to(REPO_ROOT)}")

    # Verifica que no tenga texto [PLANIFICADO] mal escrito
    # (debe ser con corchetes, como etiqueta de estado)
    bad_state_mentions = re.findall(r"\bPLANIFICADO\b(?!\])", content)
    if bad_state_mentions:
        warnings.append(
            f"PLANIFICADO sin corchetes ({len(bad_state_mentions)}x): "
            f"{md_file.relative_to(REPO_ROOT)}"
        )

    # Extrae y verifica enlaces [[wiki-style]]
    wiki_links = re.findall(r"\[\[([^\]]+)\]\]", content)
    for link in wiki_links:
        link_id = link.strip().split("|")[0]  # [[id|label]] → id
        if link_id not in VALID_IDS:
            stats["broken_links"].append(f"{md_file.relative_to(REPO_ROOT)} → [[{link_id}]]")

    # Verifica que no tenga contraseñas hardcodeadas
    secret_patterns = [
        r"password\s*=\s*[\"'][^\"']+[\"']",
        r"ConnectionString.*Password=[^;]+",
        r"api[_-]?key\s*=\s*[\"'][a-zA-Z0-9]{20,}[\"']",
    ]
    for pattern in secret_patterns:
        if re.search(pattern, content, re.IGNORECASE):
            errors.append(
                f"POSIBLE SECRETO en: {md_file.relative_to(REPO_ROOT)}"
            )


def check_required_files():
    """Verifica que los archivos obligatorios existan."""
    required = [
        REPO_ROOT / "CLAUDE.md",
        REPO_ROOT / "AGENTS.md",
        CONTEXT_ROOT / "index.md",
        CONTEXT_ROOT / "estado-actual.md",
        CONTEXT_ROOT / "_reports" / "inventario.md",
        CONTEXT_ROOT / "_reports" / "plan-de-nodos.md",
        CONTEXT_ROOT / "_reports" / "discrepancias.md",
        CONTEXT_ROOT / "_reports" / "cobertura-ids.md",
        CONTEXT_ROOT / "_reports" / "preguntas-abiertas.md",
        CONTEXT_ROOT / "servicios" / "ms1-identity.md",
        CONTEXT_ROOT / "servicios" / "ms5-pos.md",
        CONTEXT_ROOT / "servicios" / "ms4-inventory.md",
        CONTEXT_ROOT / "eventos" / "kafka-topics.md",
    ]
    for f in required:
        if not f.exists():
            errors.append(f"ARCHIVO REQUERIDO FALTANTE: {f.relative_to(REPO_ROOT)}")


def check_source_code_not_modified():
    """Verifica que NO se hayan modificado archivos de código fuente."""
    protected_dirs = ["src", "globalmart-frontend", "Docker", ".github"]
    # Esto no puede hacerse sin git en Python puro, así que solo verificamos
    # que no existan archivos nuevos en esas carpetas con extensión .md
    for d in protected_dirs:
        target = REPO_ROOT / d
        if target.exists():
            md_files = list(target.rglob("*.md"))
            if md_files:
                warnings.append(
                    f"Archivos .md encontrados en carpeta protegida {d}/: "
                    + ", ".join(str(f.relative_to(REPO_ROOT)) for f in md_files)
                )


def main():
    print("=" * 60)
    print(" GlobalMart OS — Validador del Grafo de Conocimiento")
    print(f" Repositorio: {REPO_ROOT}")
    print("=" * 60)
    print()

    # 1. Recolectar IDs para validar enlaces
    print("[1/4] Recolectando IDs de nodos...")
    collect_node_ids()
    print(f"      IDs encontrados: {len(VALID_IDS)}")

    # 2. Verificar archivos requeridos
    print("[2/4] Verificando archivos requeridos...")
    check_required_files()

    # 3. Validar cada nodo
    print("[3/4] Validando nodos...")
    for md_file in sorted(CONTEXT_ROOT.rglob("*.md")):
        validate_node(md_file)

    # 4. Verificar protección de código
    print("[4/4] Verificando que no se modifique código fuente...")
    check_source_code_not_modified()

    # Mostrar resultados
    print()
    print("=" * 60)
    print(" RESULTADOS")
    print("=" * 60)
    print(f"  Nodos totales analizados:      {stats['total_nodes']}")
    print(f"  Con frontmatter:               {stats['nodes_with_frontmatter']}")
    print(f"  Sin campo 'id':                {stats['nodes_missing_id']}")
    print(f"  Sin campo 'estado':            {stats['nodes_missing_estado']}")
    print(f"  Sin campo 'fuentes':           {stats['nodes_missing_fuentes']}")
    print(f"  Archivos vacíos:               {stats['empty_files']}")
    print(f"  IDs de nodos registrados:      {len(VALID_IDS)}")
    print()

    if stats["broken_links"]:
        print(f"  [!] ENLACES ROTOS ({len(stats['broken_links'])}):")
        for bl in stats["broken_links"][:20]:
            print(f"      -> {bl}")
        if len(stats["broken_links"]) > 20:
            print(f"      ... y {len(stats['broken_links']) - 20} mas")
    else:
        print("  [OK] Sin enlaces rotos")

    print()
    if errors:
        print(f"  [X] ERRORES ({len(errors)}):")
        for e in errors:
            print(f"      ERROR: {e}")
    else:
        print("  [OK] Sin errores criticos")

    if warnings:
        print(f"\n  [!] ADVERTENCIAS ({len(warnings)}):")
        for w in warnings[:30]:
            print(f"      WARN: {w}")
        if len(warnings) > 30:
            print(f"      ... y {len(warnings) - 30} mas")
    else:
        print("  [OK] Sin advertencias")

    print()
    print("=" * 60)
    if errors:
        print(" RESULTADO: FALLO (hay errores criticos)")
        sys.exit(1)
    else:
        print(" RESULTADO: OK")
        sys.exit(0)


if __name__ == "__main__":
    main()
