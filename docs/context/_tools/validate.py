#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
validate.py - Validador del grafo de conocimiento GlobalMart OS
Uso: python docs/context/_tools/validate.py
Salida: 0 si todo OK, 1 si hay errores
"""
import io
import os
import re
import sys
from pathlib import Path

# Force UTF-8 output on Windows
if sys.stdout.encoding != 'utf-8':
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

# Raíz del repositorio (2 niveles arriba desde _tools/)
REPO_ROOT = Path(__file__).resolve().parent.parent.parent.parent
CONTEXT_ROOT = REPO_ROOT / "docs" / "context"

errors = []
warnings = []
stats = {
    "total_nodes": 0,
    "nodes_with_frontmatter": 0,
    "nodes_missing_id": 0,
    "nodes_missing_estado": 0,
    "nodes_missing_fuentes": 0,
    "duplicate_ids": 0,
    "broken_links": [],
    "empty_files": 0,
}

VALID_IDS = set()
SEEN_IDS = {}
VALID_ESTADOS = {
    "implementado", "parcial", "planificado", "obsoleto",
    "no_verificado", "vigente"
}

IGNORED_WIKI_TARGETS = {
    "enlace", "enlaces", "link", "links", "id", "topic",
    "topic1", "id1", "ruta1", "nodo"
}


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
        if "_tools" in str(md_file):
            continue
        content = md_file.read_text(encoding="utf-8", errors="replace")
        fm = parse_frontmatter(content)
        if "id" in fm and fm["id"]:
            node_id = fm["id"].strip().strip("\"'")
            VALID_IDS.add(node_id)
        VALID_IDS.add(md_file.stem)


def check_fuentes_existence(md_file: Path, fuentes_str: str):
    """Comprueba que las rutas mencionadas en fuentes: existan en el repo."""
    # Extraer elementos dentro de corchetes o comas
    raw_items = re.findall(r'[^,\[\]\n\r]+', fuentes_str)
    for raw in raw_items:
        clean = raw.strip().strip("\"'").split("#")[0].strip()
        if not clean:
            continue
        # Ignorar descriptores textuales o comandos conocidos
        if clean.lower() in {"git log", "equipo de desarrollo uct", "equipo uct", "contextmaster"}:
            continue
        # Si parece ruta o nombre de archivo en repo
        is_path_like = any(clean.startswith(p) for p in [
            "src", "docs", "Docker", "diagramas", ".github", "globalmart-frontend"
        ]) or clean.endswith((".md", ".pdf", ".cs", ".json", ".yml", ".yaml", ".sh", ".bat", ".drawio", ".png", ".http"))

        if is_path_like:
            target = REPO_ROOT / clean
            if not target.exists():
                warnings.append(
                    f"Fuente no encontrada: '{clean}' en {md_file.relative_to(REPO_ROOT)}"
                )


def check_secrets(md_file: Path, content: str):
    """Verifica que no existan secretos reales en el contenido."""
    secret_patterns = [
        r"password\s*[:=]\s*[\"']([^\"']+)[\"']",
        r"ConnectionString.*Password=([^;\"'\s]+)",
        r"api[_-]?key\s*[:=]\s*[\"']([a-zA-Z0-9]{20,})[\"']",
        r"-----BEGIN [A-Z ]*PRIVATE KEY-----",
    ]
    known_placeholders = {
        "demo1234", "admin_password", "ci_password", "<password>",
        "min32chars", "changeme", "changeinproduction"
    }

    for pattern in secret_patterns:
        matches = re.finditer(pattern, content, re.IGNORECASE)
        for m in matches:
            val = m.group(1).lower() if m.groups() else m.group(0).lower()
            if not any(ph in val for ph in known_placeholders):
                errors.append(
                    f"POSIBLE SECRETO REAL en: {md_file.relative_to(REPO_ROOT)}"
                )


def validate_node(md_file: Path):
    """Valida un nodo markdown individual."""
    stats["total_nodes"] += 1

    content = md_file.read_text(encoding="utf-8", errors="replace")

    if not content.strip():
        stats["empty_files"] += 1
        errors.append(f"VACÍO: {md_file.relative_to(REPO_ROOT)}")
        return

    # 1. Frontmatter
    has_fm = content.startswith("---")
    if has_fm:
        stats["nodes_with_frontmatter"] += 1
        fm = parse_frontmatter(content)

        node_id = fm.get("id", "").strip().strip("\"'")
        if not node_id:
            stats["nodes_missing_id"] += 1
            warnings.append(f"SIN id: {md_file.relative_to(REPO_ROOT)}")
        else:
            # Comprobación de unicidad de ID
            if node_id in SEEN_IDS and SEEN_IDS[node_id] != md_file:
                stats["duplicate_ids"] += 1
                errors.append(
                    f"ID DUPLICADO: '{node_id}' en {md_file.relative_to(REPO_ROOT)} "
                    f"(ya definido en {SEEN_IDS[node_id].relative_to(REPO_ROOT)})"
                )
            else:
                SEEN_IDS[node_id] = md_file

        estado_val = fm.get("estado", "").strip().strip("\"'[]").lower()
        if not estado_val:
            stats["nodes_missing_estado"] += 1
            warnings.append(f"SIN estado: {md_file.relative_to(REPO_ROOT)}")
        elif estado_val not in VALID_ESTADOS:
            warnings.append(
                f"estado inválido '{estado_val}': {md_file.relative_to(REPO_ROOT)}"
            )

        fuentes_val = fm.get("fuentes", "")
        if not fuentes_val:
            stats["nodes_missing_fuentes"] += 1
            warnings.append(f"SIN fuentes: {md_file.relative_to(REPO_ROOT)}")
        else:
            # Comprobación de existencia de rutas en fuentes:
            check_fuentes_existence(md_file, fuentes_val)

    else:
        warnings.append(f"SIN frontmatter: {md_file.relative_to(REPO_ROOT)}")

    # 2. Encabezado H1
    if not re.search(r"^# .+", content, re.MULTILINE):
        warnings.append(f"SIN encabezado H1: {md_file.relative_to(REPO_ROOT)}")

    # 3. Menciones mal escritas de PLANIFICADO
    bad_state_mentions = re.findall(r"\bPLANIFICADO\b(?!\])", content)
    if bad_state_mentions:
        warnings.append(
            f"PLANIFICADO sin corchetes ({len(bad_state_mentions)}x): "
            f"{md_file.relative_to(REPO_ROOT)}"
        )

    # 4. Enlaces wiki-style [[target]]
    wiki_links = re.findall(r"\[\[([^\]]+)\]\]", content)
    for link in wiki_links:
        link_id = link.strip().split("|")[0].strip()
        # Ignorar ejemplos literales en documentación
        if link_id.lower() in IGNORED_WIKI_TARGETS:
            continue
        if link_id not in VALID_IDS:
            stats["broken_links"].append(f"{md_file.relative_to(REPO_ROOT)} -> [[{link_id}]]")

    # 5. Comprobación de secretos
    check_secrets(md_file, content)


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
    """Verifica que NO se hayan añadido archivos .md indebidos en carpetas protegidas."""
    protected_dirs = ["src", "globalmart-frontend", "Docker", ".github"]
    for d in protected_dirs:
        target = REPO_ROOT / d
        if target.exists():
            # Ignorar node_modules, .git y .webpack
            md_files = [
                f for f in target.rglob("*.md")
                if "node_modules" not in str(f) and ".git" not in str(f) and ".webpack" not in str(f)
            ]
            # Excluir READMEs documentados legítimos
            suspicious = [
                f for f in md_files
                if f.name not in ["README.md", "README_KONG.md"]
            ]
            if suspicious:
                warnings.append(
                    f"Archivos .md inesperados en carpeta protegida {d}/: "
                    + ", ".join(str(f.relative_to(REPO_ROOT)) for f in suspicious)
                )


def generate_report_text() -> str:
    """Genera el reporte estructurado."""
    lines = [
        "=" * 60,
        " GlobalMart OS — Validador del Grafo de Conocimiento",
        f" Repositorio: {REPO_ROOT}",
        "=" * 60,
        "",
        f"  Nodos analizados:              {stats['total_nodes']}",
        f"  Con frontmatter:               {stats['nodes_with_frontmatter']}",
        f"  Sin campo 'id':                {stats['nodes_missing_id']}",
        f"  Sin campo 'estado':            {stats['nodes_missing_estado']}",
        f"  Sin campo 'fuentes':           {stats['nodes_missing_fuentes']}",
        f"  IDs duplicados:                {stats['duplicate_ids']}",
        f"  Archivos vacíos:               {stats['empty_files']}",
        f"  IDs únicos registrados:        {len(VALID_IDS)}",
        "",
    ]

    if stats["broken_links"]:
        lines.append(f"  [!] ENLACES ROTOS ({len(stats['broken_links'])}):")
        for bl in stats["broken_links"][:20]:
            lines.append(f"      -> {bl}")
        if len(stats["broken_links"]) > 20:
            lines.append(f"      ... y {len(stats['broken_links']) - 20} más")
    else:
        lines.append("  [OK] Sin enlaces rotos")

    lines.append("")
    if errors:
        lines.append(f"  [X] ERRORES ({len(errors)}):")
        for e in errors:
            lines.append(f"      ERROR: {e}")
    else:
        lines.append("  [OK] Sin errores críticos")

    if warnings:
        lines.append(f"\n  [!] ADVERTENCIAS ({len(warnings)}):")
        for w in warnings[:30]:
            lines.append(f"      WARN: {w}")
        if len(warnings) > 30:
            lines.append(f"      ... y {len(warnings) - 30} más")
    else:
        lines.append("  [OK] Sin advertencias")

    lines.append("")
    lines.append("=" * 60)
    if errors:
        lines.append(" RESULTADO: FALLO (hay errores críticos)")
    else:
        lines.append(" RESULTADO: OK")
    lines.append("=" * 60)
    return "\n".join(lines)


def main():
    # 1. Recolectar IDs
    collect_node_ids()

    # 2. Verificar archivos requeridos
    check_required_files()

    # 3. Validar cada nodo (excluyendo _tools)
    for md_file in sorted(CONTEXT_ROOT.rglob("*.md")):
        if "_tools" in str(md_file):
            continue
        validate_node(md_file)

    # 4. Verificar carpetas de código
    check_source_code_not_modified()

    # 5. Generar reporte
    report = generate_report_text()
    print(report)

    # Guardar reporte en docs/context/_reports/validacion.md con UTF-8
    report_file = CONTEXT_ROOT / "_reports" / "validacion.md"
    report_content = f"""---
id: validacion
tipo: reporte
titulo: Reporte de Validación del Grafo de Conocimiento
estado: vigente
fuentes: [docs/context/_tools/validate.py]
verificado_contra_codigo: true
ultima_revision: 2026-09-29
---
# Reporte de Validación del Grafo de Conocimiento

```
{report}
```
"""
    report_file.write_text(report_content, encoding="utf-8")

    if errors:
        sys.exit(1)
    else:
        sys.exit(0)


if __name__ == "__main__":
    main()
