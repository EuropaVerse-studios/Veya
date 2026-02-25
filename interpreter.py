# interpreter.py
# L'interprete prende un'istruzione dal parser e la esegue

def execute(instruction):
    if not instruction:
        return

    cmd = instruction['command']
    args = instruction['args']

    if cmd == 'print':
        print(" ".join(map(str, args)))
    elif cmd == 'add':
        result = sum(args)
        print(result)
    elif cmd == 'multiply':
        result = 1
        for x in args:
            result *= x
        print(result)
    else:
        print(f"Comando sconosciuto: {cmd}")