#!/usr/bin/env python3
"""
sprintplan_to_youtrack.py
==========================

Convierte el archivo `GlobalMart_SprintPlan.md` (o cualquier plan de sprints con
el mismo formato) en un CSV listo para importar en YouTrack.

Reglas de parseo implementadas (ver README / explicación adjunta):

1. El sprint activo se detecta con encabezados `# SPRINT N ...`.
2. En el Sprint 1, el responsable/tipo/prioridad viven en un encabezado H3:
   `### Nombre — Tipo · Microservicio(s) · Tiempo · Prioridad`
   y las subtareas están en la tabla de 4 columnas que viene justo debajo:
   `| # | Subtarea | Dificultad | Tiempo |`
3. En Sprints 2 y 3, cada tarea es una fila de la tabla de 6 columnas:
   `| Persona | Tipo | Tarea | Microservicio | Horas | Prioridad |`
4. Cualquier tabla que NO tenga exactamente 4 columnas (dentro del Sprint 1) o
   exactamente 6 columnas (dentro de Sprints 2/3) se ignora explícitamente
   (tablas de "Integrantes del Equipo", "Leyenda", "Resumen Global", etc.).
   Además, se valida que los encabezados de columna contengan las palabras
   clave esperadas, para no confundir una tabla de 4 o 6 columnas cualquiera
   con una tabla de tareas real.
5. Los tiempos "45 min" / "5 h" / "4 h 40 min" se normalizan al formato de
   YouTrack: "45m" / "5h" / "4h40m".

Uso:
    python sprintplan_to_youtrack.py GlobalMart_SprintPlan.md -o tareas_youtrack.csv

El script es tolerante a errores: si encuentra una tabla o un encabezado que
no puede interpretar de forma segura, la descarta y deja un registro (log)
explicando por qué, en lugar de fallar o de generar filas basura.
"""

from __future__ import annotations

import argparse
import csv
import logging
import re
import sys
import unicodedata
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

# --------------------------------------------------------------------------- #
# Configuración de logging
# --------------------------------------------------------------------------- #

logger = logging.getLogger("sprintplan_to_youtrack")


def setup_logging(verbose: bool) -> None:
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(levelname)-8s | %(message)s",
        stream=sys.stderr,
    )


# --------------------------------------------------------------------------- #
# Utilidades de normalización de texto
# --------------------------------------------------------------------------- #

def strip_markdown_bold(text: str) -> str:
    """Quita ** de negritas de markdown, p.ej. '**Rodrigo**' -> 'Rodrigo'."""
    return re.sub(r"\*\*(.+?)\*\*", r"\1", text).strip()


def strip_emoji_and_symbols(text: str) -> str:
    """
    Elimina emojis / símbolos decorativos (🔴 🟡 🟢) dejando solo el texto
    "humano" (Must, Should, Could, Baja, Media, Alta, etc.).
    """
    cleaned = "".join(
        ch
        for ch in text
        if not unicodedata.category(ch).startswith("So")  # Symbol, other (emoji)
    )
    return cleaned.strip(" \t·-—")


def clean_cell(text: str) -> str:
    """Limpieza genérica de una celda de tabla markdown."""
    text = text.strip()
    text = strip_markdown_bold(text)
    # Colapsa espacios múltiples que a veces deja el markdown
    text = re.sub(r"\s+", " ", text)
    return text.strip()


TIME_TOKEN_RE = re.compile(r"(\d+)\s*(h|hrs?|horas?|min|m|mins?|minutos?)\b", re.IGNORECASE)


def normalize_time(raw: str) -> str:
    """
    Convierte tiempos en lenguaje natural al formato de estimación de
    YouTrack: '45 min' -> '45m', '5 h' -> '5h', '4 h 40 min' -> '4h40m'.

    Si no se reconoce ningún patrón de tiempo, se devuelve el texto original
    limpio (para no perder información silenciosamente) y se registra un
    warning.
    """
    if not raw:
        return ""

    raw_clean = raw.strip()
    hours = 0
    minutes = 0
    found_any = False

    for value, unit in TIME_TOKEN_RE.findall(raw_clean):
        found_any = True
        unit_lower = unit.lower()
        if unit_lower.startswith("h"):
            hours += int(value)
        else:
            minutes += int(value)

    if not found_any:
        logger.warning("No se pudo interpretar el tiempo %r; se deja tal cual.", raw_clean)
        return raw_clean

    parts = []
    if hours:
        parts.append(f"{hours}h")
    if minutes:
        parts.append(f"{minutes}m")
    return "".join(parts) if parts else "0m"


# --------------------------------------------------------------------------- #
# Mapeos ajustados a la instancia real de YouTrack del usuario (Taller
# integracion 3). Si cambian los nombres/valores en YouTrack, solo hay que
# editar estos diccionarios.
# --------------------------------------------------------------------------- #

# Dificultad del plan (Baja/Media/Alta) -> valores reales del campo
# "Prioridad" en YouTrack. Esta es la fuente que ahora usamos para el campo
# Prioridad en las tareas de Sprint 1 (cada subtarea trae su propia
# dificultad, a diferencia de la prioridad del encabezado que es la misma
# para todas las subtareas de esa persona).
DIFFICULTY_PRIORITY_MAP = {
    "Baja": "Facil",
    "Media": "Medio",
    "Alta": "Alta",
}

# Prioridad del plan (Must/Should/Could) -> valores reales del campo
# "Prioridad" en YouTrack. Se usa solo como respaldo cuando la tarea no
# tiene Dificultad propia (p. ej. filas de Sprint 2/3).
PRIORITY_MAP = {
    "Must": "Alta",
    "Should": "Medio",
    "Could": "Facil",
}

# Nombre de pila (tal como aparece en el plan de sprints) -> login real de
# YouTrack, según la lista de "Usuario asignado" de tu proyecto.
# Las claves están sin tilde porque la búsqueda normaliza acentos antes de
# mirar el diccionario (así "Martín" y "Martin" resuelven al mismo login).
ASSIGNEE_LOGIN_MAP = {
    "Rodrigo": "admin",          # rpedraza2024@alu.uct.cl
    "Martin": "msanhueza2024",   # Martin Sanhueza
    "Nicolas": "nmartinez2024",
    "Sebastian": "spereda2024",
    "Daniel": "dprado2024",      # Daniel Prado
    "Diego": "dleiva2024",
}


def strip_accents(text: str) -> str:
    """'Martín' -> 'Martin', 'Nicolás' -> 'Nicolas' (para matchear nombres con/sin tilde)."""
    normalized = unicodedata.normalize("NFKD", text)
    return "".join(ch for ch in normalized if not unicodedata.combining(ch))


KNOWN_WORK_AREAS = ["Backend", "Frontend", "DevOps", "Testing"]

# Códigos de una letra usados en el formato de tabla única de 6 columnas
# (Sprint 2/3 del plan original GlobalMart).
WORK_AREA_LETTER_CODES = {
    "B": "Backend",
    "F": "Frontend",
    "T": "Testing",
    "D": "DevOps",
}


def map_type(raw_type: str) -> str:
    """
    El plan de sprints usa 'Tipo' para Backend/Frontend/Testing/DevOps, pero
    en tu YouTrack el campo 'Tipo' es el tipo de incidencia (Tarea, Bug, ...),
    un concepto distinto. Por eso este valor se manda al campo propio
    "Área de Trabajo".

    Algunos encabezados no siguen el patrón limpio 'Tipo · MS · Tiempo' (p.ej.
    'Testing E2E Final Sprint 2 · 5 h', que no tiene segmento de
    Microservicio separado), así que en vez de usar el segmento completo tal
    cual, buscamos cuál de las categorías conocidas aparece en el texto.
    """
    cleaned = strip_emoji_and_symbols(raw_type).strip()

    letter_match = WORK_AREA_LETTER_CODES.get(cleaned.upper())
    if letter_match:
        return letter_match

    for area in KNOWN_WORK_AREAS:
        if area.lower() in cleaned.lower():
            return area

    logger.warning("Área de trabajo '%s' no coincide con ninguna categoría conocida %s; se deja tal cual.", cleaned, KNOWN_WORK_AREAS)
    return cleaned


def map_priority(raw_priority: str) -> str:
    """Respaldo: mapea la Prioridad de encabezado (Must/Should/Could) cuando no hay Dificultad."""
    cleaned = strip_emoji_and_symbols(raw_priority).strip()
    mapped = PRIORITY_MAP.get(cleaned)
    if mapped is None:
        logger.warning("Prioridad '%s' sin mapeo conocido; se deja tal cual (revisa PRIORITY_MAP).", cleaned)
        return cleaned
    return mapped


def map_priority_from_difficulty(raw_difficulty: str) -> str:
    """Fuente principal: mapea la Dificultad (Baja/Media/Alta) al campo Prioridad de YouTrack."""
    cleaned = strip_emoji_and_symbols(raw_difficulty).strip()
    mapped = DIFFICULTY_PRIORITY_MAP.get(cleaned)
    if mapped is None:
        logger.warning("Dificultad '%s' sin mapeo conocido; se deja tal cual (revisa DIFFICULTY_PRIORITY_MAP).", cleaned)
        return cleaned
    return mapped


def map_assignee(raw_name: str) -> str:
    cleaned = clean_cell(raw_name)
    login = ASSIGNEE_LOGIN_MAP.get(cleaned) or ASSIGNEE_LOGIN_MAP.get(strip_accents(cleaned))
    if login is None:
        logger.warning("Asignado '%s' sin login mapeado; se deja el nombre tal cual (revisa ASSIGNEE_LOGIN_MAP).", cleaned)
        return cleaned
    return login


WEEK_SHORT_RE = re.compile(r"(Semana\s+\d+)", re.IGNORECASE)


def short_week_label(full_week: str) -> str:
    """'Semana 1 — Scaffolding y Arquitectura Base' -> 'Semana 1' (para usar como tag corto)."""
    match = WEEK_SHORT_RE.search(full_week)
    return match.group(1) if match else full_week


# --------------------------------------------------------------------------- #
# Modelo de datos
# --------------------------------------------------------------------------- #

@dataclass
class Task:
    sprint: int
    week: str
    assignee: str
    task_type: str
    summary: str
    subsystem: str
    estimation_raw: str
    priority: str
    difficulty: str = ""

    def to_row(self) -> dict:
        week_short = short_week_label(self.week)
        difficulty_clean = strip_emoji_and_symbols(self.difficulty)
        area_trabajo = map_type(self.task_type)

        # Prioridad: si la tarea trae Dificultad propia (Sprint 1), esa manda.
        # Si no (Sprint 2/3), usamos la Prioridad del encabezado como respaldo.
        if difficulty_clean:
            priority_value = map_priority_from_difficulty(difficulty_clean)
        else:
            priority_value = map_priority(self.priority)

        description_lines = []
        if self.subsystem:
            description_lines.append(f"Microservicio/Subsistema: {self.subsystem}")
        priority_clean = strip_emoji_and_symbols(self.priority)
        if priority_clean:
            description_lines.append(f"Prioridad original del plan (Must/Should/Could): {priority_clean}")

        return {
            "Summary": self.summary,
            "Description": "\n".join(description_lines),
            "Assignee": map_assignee(self.assignee),
            "Priority": priority_value,
            "Sprints": f"Sprint {self.sprint}",
            "Semana": week_short,
            "AreaTrabajo": area_trabajo,
            "Tags": week_short,
            "Estimation": normalize_time(self.estimation_raw),
        }


# --------------------------------------------------------------------------- #
# Parseo de tablas markdown
# --------------------------------------------------------------------------- #

def split_table_row(line: str) -> list[str]:
    """Divide una fila `| a | b | c |` en celdas limpias ['a', 'b', 'c']."""
    stripped = line.strip()
    if stripped.startswith("|"):
        stripped = stripped[1:]
    if stripped.endswith("|"):
        stripped = stripped[:-1]
    return [clean_cell(cell) for cell in stripped.split("|")]


def is_separator_row(cells: list[str]) -> bool:
    """Detecta la fila `|---|---|---|` que separa encabezado de datos."""
    if not cells:
        return False
    return all(re.fullmatch(r":?-{1,}:?", c.replace(" ", "")) for c in cells if c != "")


@dataclass
class TableBlock:
    header: list[str]
    rows: list[list[str]]
    start_line: int


def collect_table_blocks(lines: list[str]) -> dict[int, TableBlock]:
    """
    Recorre todas las líneas y agrupa bloques de tabla markdown consecutivos.
    Devuelve un diccionario {línea_de_inicio: TableBlock} para poder
    intercalarlos con el resto del procesamiento línea por línea.
    """
    blocks: dict[int, TableBlock] = {}
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        if line.strip().startswith("|") and line.strip().endswith("|"):
            block_start = i
            table_lines = []
            while i < n and lines[i].strip().startswith("|") and lines[i].strip().endswith("|"):
                table_lines.append(lines[i])
                i += 1

            if len(table_lines) < 2:
                # No hay ni encabezado + separador; no es una tabla real.
                continue

            header_cells = split_table_row(table_lines[0])
            sep_cells = split_table_row(table_lines[1])

            if not is_separator_row(sep_cells):
                logger.debug(
                    "Línea %d: bloque con '|' pero sin fila separadora válida; se ignora.",
                    block_start + 1,
                )
                continue

            data_rows = [split_table_row(l) for l in table_lines[2:]]
            blocks[block_start] = TableBlock(
                header=header_cells, rows=data_rows, start_line=block_start
            )
        else:
            i += 1
    return blocks


# --------------------------------------------------------------------------- #
# Reconocimiento de encabezados de contexto
# --------------------------------------------------------------------------- #

SPRINT_HEADER_RE = re.compile(r"^#\s*SPRINT\s+(\d+)", re.IGNORECASE)
WEEK_HEADER_RE = re.compile(r"^##\s+(.+)$")
# "### Nombre — Tipo · MS... · Tiempo · Prioridad"
ASSIGNEE_HEADER_RE = re.compile(r"^###\s+(.+?)\s+[—-]\s+(.+)$")

EXPECTED_SPRINT1_HEADER_KEYWORDS = {"subtarea", "tiempo"}
EXPECTED_SPRINT23_HEADER_KEYWORDS = {"persona", "tarea", "horas"}


def header_matches_keywords(header_cells: list[str], keywords: set[str]) -> bool:
    lowered = " ".join(h.lower() for h in header_cells)
    return all(k in lowered for k in keywords)


@dataclass
class AssigneeContext:
    name: str
    task_type: str
    subsystem: str
    priority: str


def parse_assignee_header(line: str) -> Optional[AssigneeContext]:
    """
    Intenta interpretar un encabezado H3 de responsable, en cualquiera de dos
    variantes vistas en los planes de sprint:

    A) 'Nombre — Tipo · Microservicio(s) · Tiempo · Prioridad'   (4 segmentos)
    B) 'Nombre — Tipo · Microservicio(s) · Tiempo'               (3 segmentos,
       sin Prioridad; usada en tareasSprint2.md — en ese caso la Prioridad
       queda vacía y se resuelve más adelante desde la Dificultad de cada
       subtarea, que siempre está presente en este formato).

    Devuelve None si el encabezado no matchea ninguna variante (por ejemplo
    '### Semanas 1-4 · 120 h', que no tiene responsable y debe ignorarse).
    """
    match = ASSIGNEE_HEADER_RE.match(line.strip())
    if not match:
        return None

    name_part, rest = match.groups()
    segments = [s.strip() for s in rest.split("·")]

    if len(segments) < 2:
        logger.debug("Encabezado '%s' no tiene el formato esperado (menos de 2 segmentos tras '·'); se ignora como asignación.", line.strip())
        return None

    task_type = segments[0]

    # Variante A: el ÚLTIMO segmento no es un tiempo -> es la Prioridad,
    # y el PENÚLTIMO sí debe ser un tiempo.
    if len(segments) >= 3 and not TIME_TOKEN_RE.search(segments[-1]) and TIME_TOKEN_RE.search(segments[-2]):
        priority = segments[-1]
        subsystem_segments = segments[1:-2]
    # Variante B: el ÚLTIMO segmento ya es el tiempo -> no hay Prioridad.
    elif TIME_TOKEN_RE.search(segments[-1]):
        priority = ""
        subsystem_segments = segments[1:-1]
    else:
        logger.debug(
            "Encabezado '%s': no se encontró un segmento de tiempo reconocible al final; se ignora como asignación.",
            line.strip(),
        )
        return None

    subsystem = " · ".join(subsystem_segments) if subsystem_segments else ""

    return AssigneeContext(
        name=clean_cell(name_part),
        task_type=task_type,
        subsystem=subsystem,
        priority=priority,
    )


# --------------------------------------------------------------------------- #
# Parser principal
# --------------------------------------------------------------------------- #

def parse_sprint_plan(md_text: str) -> list[Task]:
    lines = md_text.splitlines()
    table_blocks = collect_table_blocks(lines)

    tasks: list[Task] = []

    current_sprint: Optional[int] = None
    current_week: str = ""
    current_assignee: Optional[AssigneeContext] = None

    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]

        sprint_match = SPRINT_HEADER_RE.match(line.strip())
        if sprint_match:
            current_sprint = int(sprint_match.group(1))
            current_assignee = None
            logger.info("Detectado inicio de Sprint %d.", current_sprint)
            i += 1
            continue

        if line.strip().startswith("## "):
            week_match = WEEK_HEADER_RE.match(line.strip())
            if week_match:
                current_week = clean_cell(week_match.group(1))
            i += 1
            continue

        if line.strip().startswith("### "):
            ctx = parse_assignee_header(line)
            if ctx is not None:
                current_assignee = ctx
                logger.debug(
                    "Sprint %s · Semana '%s': nuevo responsable detectado -> %s (%s, prioridad=%r)",
                    current_sprint, current_week, ctx.name, ctx.task_type, ctx.priority,
                )
            i += 1
            continue

        if i in table_blocks:
            block = table_blocks[i]
            n_cols = len(block.header)

            if n_cols == 4 and header_matches_keywords(block.header, EXPECTED_SPRINT1_HEADER_KEYWORDS):
                if current_assignee is None:
                    logger.warning(
                        "Línea %d: tabla de subtareas encontrada sin responsable activo; se ignora para evitar datos inconsistentes.",
                        block.start_line + 1,
                    )
                else:
                    for row in block.rows:
                        if len(row) != 4:
                            logger.warning(
                                "Línea %d: fila con %d columnas (se esperaban 4); se ignora: %s",
                                block.start_line + 1, len(row), row,
                            )
                            continue
                        _, subtarea, dificultad, tiempo = row
                        if not subtarea:
                            continue
                        tasks.append(
                            Task(
                                sprint=current_sprint,
                                week=current_week,
                                assignee=current_assignee.name,
                                task_type=current_assignee.task_type,
                                summary=subtarea,
                                subsystem=current_assignee.subsystem,
                                estimation_raw=tiempo,
                                priority=current_assignee.priority,
                                difficulty=dificultad,
                            )
                        )

            elif n_cols == 6 and header_matches_keywords(block.header, EXPECTED_SPRINT23_HEADER_KEYWORDS):
                for row in block.rows:
                    if len(row) != 6:
                        logger.warning(
                            "Línea %d: fila con %d columnas (se esperaban 6); se ignora: %s",
                            block.start_line + 1, len(row), row,
                        )
                        continue
                    persona, tipo, tarea, microservicio, horas, prioridad = row
                    if not persona:
                        continue
                    tasks.append(
                        Task(
                            sprint=current_sprint,
                            week=current_week,
                            assignee=persona,
                            task_type=tipo,
                            summary=tarea,
                            subsystem=microservicio,
                            estimation_raw=horas,
                            priority=prioridad,
                        )
                    )
            else:
                logger.debug(
                    "Línea %d: tabla de %d columnas ignorada (fuera de contexto de sprint válido o tamaño no esperado).",
                    block.start_line + 1, n_cols,
                )

            # Saltamos todas las líneas que forman parte de esta tabla
            # (encabezado + separador + filas).
            i = block.start_line + 2 + len(block.rows)
            continue

        i += 1

    return tasks


# --------------------------------------------------------------------------- #
# Escritura de CSV
# --------------------------------------------------------------------------- #

CSV_FIELDNAMES = [
    "Summary",
    "Description",
    "Assignee",
    "Priority",
    "Sprints",
    "Semana",
    "AreaTrabajo",
    "Tags",
    "Estimation",
]


def write_csv(tasks: list[Task], output_path: Path) -> None:
    with output_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=CSV_FIELDNAMES)
        writer.writeheader()
        for task in tasks:
            writer.writerow(task.to_row())


# --------------------------------------------------------------------------- #
# CLI
# --------------------------------------------------------------------------- #

def main() -> int:
    parser = argparse.ArgumentParser(
        description="Convierte un plan de sprints en Markdown a un CSV importable en YouTrack."
    )
    parser.add_argument("input", type=Path, help="Ruta al archivo .md del plan de sprints")
    parser.add_argument(
        "-o", "--output", type=Path, default=Path("tareas_youtrack.csv"),
        help="Ruta del CSV de salida (default: tareas_youtrack.csv)",
    )
    parser.add_argument(
        "-v", "--verbose", action="store_true",
        help="Muestra logs de depuración detallados (qué tablas/encabezados se ignoraron y por qué)",
    )
    parser.add_argument(
        "--sprint", type=int, default=None,
        help="Si se indica, exporta solo las tareas de ese número de sprint (ej. --sprint 1)",
    )
    args = parser.parse_args()

    setup_logging(args.verbose)

    if not args.input.exists():
        logger.error("El archivo de entrada no existe: %s", args.input)
        return 1

    try:
        md_text = args.input.read_text(encoding="utf-8")
    except UnicodeDecodeError as exc:
        logger.error("No se pudo leer el archivo como UTF-8: %s", exc)
        return 1

    tasks = parse_sprint_plan(md_text)

    if args.sprint is not None:
        before = len(tasks)
        tasks = [t for t in tasks if t.sprint == args.sprint]
        logger.info("Filtro --sprint %d aplicado: %d -> %d tareas.", args.sprint, before, len(tasks))

    if not tasks:
        logger.warning("No se extrajo ninguna tarea. Revisa el formato del archivo de entrada.")
        return 0

    write_csv(tasks, args.output)

    by_sprint: dict[int, int] = {}
    for t in tasks:
        by_sprint[t.sprint] = by_sprint.get(t.sprint, 0) + 1

    logger.info("Total de tareas extraídas: %d", len(tasks))
    for sprint_num in sorted(by_sprint):
        logger.info("  Sprint %d: %d tareas", sprint_num, by_sprint[sprint_num])
    logger.info("CSV escrito en: %s", args.output.resolve())

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
