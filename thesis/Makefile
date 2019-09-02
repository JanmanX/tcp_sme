.PHONY: all

default: all

all:
	latexmk -pdf -shell-escape -recorder thesis.tex
	makeglossaries thesis
	latexmk -pdf -shell-escape -recorder -g thesis.tex

clean:
	-git clean -Xdf
	-rm -rvf *.pdf *.log *.toc *.bbl *.aux *.blg *.out *.fdb_latexmk *.fls
