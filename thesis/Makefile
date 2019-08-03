.PHONY: all

default: all

all:
	makeglossaries thesis
	latexmk -pdf -shell-escape thesis.tex

clean:
	-rm -rvf *.pdf *.log *.toc *.bbl *.aux *.blg *.out *.fdb_latexmk *.fls
