# parser.py
# Il parser prende i token e li organizza in "istruzione" che interpreterà dopo

def parse(tokens):
    """
    tokens: lista di token dal lexer
    ritorna: dizionario con comando e argomenti
    """
    if not tokens:
        return None

    # Il primo token è sempre il comando
    cmd_type, cmd_value = tokens[0]
    if cmd_type != 'COMMAND':
        return None

    # Tutti gli altri token sono argomenti
    args = [value for token_type, value in tokens[1:]]
    
    return {
        'command': cmd_value,
        'args': args
    }