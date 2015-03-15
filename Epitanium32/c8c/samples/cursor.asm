mov v0, 0
font v0
mov v0, 1F
mov v1, F
mov v2, 1
mov v3, 2
mov v4, 3
mov v5, 4
cls
sprite v0, v1, 1
key vE
skeq vE, v2
jmp 10
add v0, v2
jmp 1D
skeq vE, v3
jmp 14
sub v0, v2
jmp 1D
skeq vE, v4
jmp 18
add v1, v2
jmp 1D
skeq vE, v5
jmp 1C
sub v1, v2
jmp 1D
jmp B
jmp 9