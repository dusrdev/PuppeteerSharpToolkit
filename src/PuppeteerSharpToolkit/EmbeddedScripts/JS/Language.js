(...languages) => {
        // Flatten and sanitize to always get an array of non-empty strings.
        const list = (Array.isArray(languages[0]) ? languages[0] : languages)
            .flat()
            .filter(x => typeof x === 'string' && x.trim().length > 0);

        utils.replaceGetterWithProxy(
            Object.getPrototypeOf(navigator),
            'languages',
            utils.makeHandler().getterValue(Object.freeze(list))
        )
    }
    //# sourceURL=Language.js