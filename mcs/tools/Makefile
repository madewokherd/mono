thisdir = tools

# Wine Mono disabled tools: mono-xmltool dtd2rng mdoc mod cccheck ccrewrite sqlmetal aprofutil mono-service

net_4_5_dirs := \
	al		\
	linker		\
	culevel		\
	genxs		\
	mkbundle	\
	monop		\
	mono-xsd	\
	resgen		\
	wsdl		\
	xbuild		\
	csharp		\
	corcompare	\
	mono-api-diff \
	mono-api-html \
	compiler-tester	\
	mono-shlib-cop	\
	sgen		\
	mconfig		\
	installutil	\
	nunitreport	\
	pdb2mdb		\
	sqlsharp	\
	svcutil		\
	ictool		\
	disco		\
	soapsuds	\
	browsercaps-updater	\
	cil-strip	\
	macpack		\
	dtd2xsd		\
	installvst	\
	lc		\
	mono-configuration-crypto \
	security	\
	mdbrebase	\
	mdb2ppdb	\
	ikdasm		\
	mono-symbolicate	\
	linker-analyzer \
	gacutil

build_SUBDIRS =
build_PARALLEL_SUBDIRS := resgen gacutil security culevel commoncryptogenerator resx2sr linker cil-strip corcompare mono-api-diff mono-api-html
monodroid_tools_SUBDIRS =
monodroid_tools_PARALLEL_SUBDIRS = aprofutil cil-strip linker-analyzer mkbundle mdoc mono-symbolicate corcompare mono-api-diff mono-api-html pdb2mdb nunit-lite
monodroid_SUBDIRS = nunit-lite
monotouch_SUBDIRS = nunit-lite
monotouch_tv_SUBDIRS = nunit-lite
monotouch_watch_SUBDIRS = nunit-lite
monotouch_tools_SUBDIRS =
monotouch_tools_PARALLEL_SUBDIRS = corcompare mono-api-html
xammac_SUBDIRS = nunit-lite
xammac_net_4_5_SUBDIRS = nunit-lite
net_4_x_SUBDIRS =
net_4_x_PARALLEL_SUBDIRS = $(net_4_5_dirs)
wasm_tools_SUBDIRS =
wasm_tools_PARALLEL_SUBDIRS = linker wasm-tuner cil-strip

DIST_SUBDIRS = $(net_4_5_dirs) cil-stringreplacer commoncryptogenerator resx2sr gensources

include ../build/rules.make

DISTFILES = \
	removecomments.sh

test-local run-test-local run-test-ondotnet-local all-local install-local uninstall-local doc-update-local:
	@:

dist-local: dist-default

clean-local:
	rm -f *.exe *.dll *.pdb cormissing.xml
