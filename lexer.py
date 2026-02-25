# lexer.py
# Il compito del lexer è prendere una riga di testo e trasformarla in "token" leggibili dal parser
# Ogni token può essere: comando, numero o stringa

def tokenize(line):
    """
    line: una riga di codice Veya (es: 'add 5 3')
    ritorna: lista di token
    """
    tokens = []
    parts = line.strip().split()  # divide la riga per spazi
    
    for part in parts:
        # Se è un numero, lo trasformiamo in int
        if part.isdigit():
            tokens.append(('NUMBER', int(part)))
        # Se è una stringa tra virgolette, togli le virgolette
        elif part.startswith('"') and part.endswith('"'):
            tokens.append(('STRING', part[1:-1]))
        else:
            # Altrimenti lo consideriamo un comando
            tokens.append(('COMMAND', part))
    return tokens