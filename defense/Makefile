.phony: all

all:
	latexmk -pdf -shell-escape -recorder slides.tex

clean:
	-git clean -Xdf
	-rm -rvf *.pdf *.log *.toc *.bbl *.aux *.blg *.out *.fdb_latexmk *.fls

