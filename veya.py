# veya.py

import sys  # serve per leggere gli argomenti passati da terminale

from lexer import tokenize
from parser import parse
from interpreter import execute


def run_file(filename):
    """
    Questa funzione apre un file .veya
    e lo esegue riga per riga
    """

    # open apre il file in modalità lettura ("r")
    with open(filename, "r") as file:
        # file è ora una sequenza di righe
        for line in file:
            tokens = tokenize(line)
            instruction = parse(tokens)
            execute(instruction)


def run_repl():
    """
    REPL = Read Evaluate Print Loop
    È la modalità interattiva (tipo Python)
    """

    print("Benvenuto in Veya!")
    print("Scrivi comandi. Digita 'exit' per uscire.")

    while True:
        line = input(">> ")

        if line.lower() in ("exit", "quit"):
            break

        tokens = tokenize(line)
        instruction = parse(tokens)
        execute(instruction)


# Questo blocco viene eseguito solo se lanci direttamente veya.py
if __name__ == "__main__":

    # sys.argv è una lista:
    # [nome_script, argomento1, argomento2, ...]
    # esempio:
    # python veya.py prova.veya
    # sys.argv diventa:
    # ["veya.py", "prova.veya"]

    if len(sys.argv) > 1:
        # Se c'è un argomento, lo consideriamo un file
        filename = sys.argv[1]
        run_file(filename)
    else:
        # Se non c'è nessun file, avvia modalità interattiva
        run_repl()