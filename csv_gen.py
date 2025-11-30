import random
import csv
import os

# --- PARAMETRY ---
days = 7
shifts = 3
employees = 10  # liczba pracowników

folder = "data_csv"
os.makedirs(folder, exist_ok=True)
filename = os.path.join(folder, f"grafik_{days}d_{shifts}s_{employees}emp.csv")

# --- GENEROWANIE CSV ---
with open(filename, "w", newline="") as csvfile:
    writer = csv.writer(csvfile)

    # --- 1. Nagłówki wymagań ---
    req_headers = []
    req_values = []

    for day in range(1, days + 1):
        for shift in range(1, shifts + 1):
            req_headers.append(f"req_{day}d_{shift}s")
            min_required = max(1, employees // 2)
            if employees==1:
                employees=2
            max_required = employees - 1
            req_values.append(random.randint(min_required, max_required))

    writer.writerow(req_headers)
    writer.writerow(req_values)

    # --- 2. Nagłówki preferencji ---
    pref_headers = [f"pref_{day}d_{shift}s" 
                    for day in range(1, days + 1) 
                    for shift in range(1, shifts + 1)]
    writer.writerow(pref_headers)

    # --- 3. Preferencje pracowników (każda linia = jeden pracownik) ---
    for emp in range(1, employees + 1):
        row = [random.randint(0, 1) for _ in pref_headers]
        writer.writerow(row)

print("CSV wygenerowano:", filename)
