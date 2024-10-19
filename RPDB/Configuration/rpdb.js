define(['loading', 'emby-input', 'emby-select', 'emby-checkbox', 'emby-button'], function (loading) {
    'use strict';

    var globalKey = ''

    function loadPage(page, config, cb) {

        page.querySelector('#txtRpdbApiKey').value = config.UserApiKey || '';

        if (config.UserApiKey) {
            onLoadKey(page)
        }

        page.querySelector('#posterType').value = config.PosterType || 'poster-default';

        page.querySelector('#textless').checked = !!(config.Textless === '1')

        page.querySelector('#backdrops').checked = !!(config.Backdrops === '1')

        page.querySelector('#posterLang').value = config.PosterLang || 'en';

        if (config.PosterType === 'rating-order') {

            if (config.FirstRating && config.FirstRating !== 'imdb') {
                page.querySelector('#firstRating').value = config.FirstRating
            }

            if (config.SecondRating && config.SecondRating !== 'tomatoes-critics') {
                page.querySelector('#secondRating').value = config.SecondRating
            }

            if (config.ThirdRating && config.ThirdRating !== 'metacritic-critics') {
                page.querySelector('#thirdRating').value = config.ThirdRating;
            }

            if (config.FirstBackupRating && config.FirstBackupRating !== 'none') {
                page.querySelector('#firstBackupRating').value = config.FirstBackupRating;
            }

            if (config.SecondBackupRating && config.SecondBackupRating !== 'none') {
                page.querySelector('#secondBackupRating').value = config.SecondBackupRating;
            }

            page.querySelectorAll('.customOrderSettings')[0].style.display = 'block'

        }

        page.querySelector('#videoQuality').checked = !!(config.VideoQuality === '1')
        page.querySelector('#colorRange').checked = !!(config.ColorRange === '1')
        page.querySelector('#audioChannels').checked = !!(config.AudioChannels === '1')

        loading.hide();

        cb();
    }

    function onSubmit(e, page) {

        e.preventDefault();

        if (!globalKey)
            return;

        loading.show();

        var form = this;

        ApiClient.getNamedConfiguration("rpdb").then(function (config) {

            config.UserApiKey = globalKey;

            config.PosterType = page.querySelector('#posterType').value;

            config.Textless = page.querySelector('#textless:checked') !== null ? '1' : '0'

            config.Backdrops = page.querySelector('#backdrops:checked') !== null ? '1' : '0'

            config.PosterLang = page.querySelector('#posterLang').value;

            config.FirstRating = page.querySelector('#firstRating').value;
            config.SecondRating = page.querySelector('#secondRating').value;
            config.ThirdRating = page.querySelector('#thirdRating').value;
            config.FirstBackupRating = page.querySelector('#firstBackupRating').value;
            config.SecondBackupRating = page.querySelector('#secondBackupRating').value;

            config.VideoQuality = page.querySelector('#videoQuality:checked') !== null ? '1' : '0'
            config.ColorRange = page.querySelector('#colorRange:checked') !== null ? '1' : '0'
            config.AudioChannels = page.querySelector('#audioChannels:checked') !== null ? '1' : '0'

            ApiClient.updateNamedConfiguration("rpdb", config).then(Dashboard.processServerConfigurationUpdateResult);
        });

        // Disable default form submission
        return false;
    }

    function onLoadKey(page) {
        globalKey = ''
        var key = page.querySelector('#txtRpdbApiKey').value || '';
        page.querySelectorAll('.invalidKey')[0].style.display = 'none'
        if (key && key.match(/^t[0-9]\-/)) {
            fetch(`https://api.ratingposterdb.com/${key}/isValid`).then(function(resp) {
                return resp.json()
            }).then(function(obj) {
                if ((obj || {}).valid) {
                    globalKey = key
                    if (globalKey.startsWith('t0-')) {
                        page.querySelector('#posterType').disabled = true
                        page.querySelector('#textless').disabled = true
                        page.querySelector('#backdrops').disabled = true
                        page.querySelector('#posterLang').disabled = true
                        page.querySelector('#videoQuality').disabled = true
                        page.querySelector('#colorRange').disabled = true
                        page.querySelector('#audioChannels').disabled = true
                    } else if (globalKey.startsWith('t1-')) {
                        page.querySelector('#posterType').disabled = false
                        page.querySelector('#textless').disabled = false
                        page.querySelector('#backdrops').disabled = true
                        page.querySelector('#posterLang').disabled = true
                        page.querySelector('#videoQuality').disabled = true
                        page.querySelector('#colorRange').disabled = true
                        page.querySelector('#audioChannels').disabled = true
                    } else if (globalKey.startsWith('t2-')) {
                        page.querySelector('#posterType').disabled = false
                        page.querySelector('#textless').disabled = false
                        page.querySelector('#backdrops').disabled = false
                        page.querySelector('#posterLang').disabled = false
                        page.querySelector('#videoQuality').disabled = true
                        page.querySelector('#colorRange').disabled = true
                        page.querySelector('#audioChannels').disabled = true
                    } else if (globalKey.match(/^t[3-9]\-/)) {
                        page.querySelector('#posterType').disabled = false
                        page.querySelector('#textless').disabled = false
                        page.querySelector('#backdrops').disabled = false
                        page.querySelector('#posterLang').disabled = false
                        page.querySelector('#videoQuality').disabled = false
                        page.querySelector('#colorRange').disabled = false
                        page.querySelector('#audioChannels').disabled = false
                    }
                    page.querySelectorAll('.rpdbSettings')[0].style.display = 'block'
                    page.querySelectorAll('.load-key')[0].style.display = 'none'
                    page.querySelectorAll('.keyHandler')[0].style.display = 'none'
                } else {
                    page.querySelectorAll('.invalidKey')[0].style.display = 'block'
                }
            }).catch(function(err) {
                page.querySelectorAll('.invalidKey')[0].style.display = 'block'
            })
        } else {
            page.querySelectorAll('.invalidKey')[0].style.display = 'block'
        }
    }

    function onPosterTypeChanged(page) {
        var posterType = page.querySelector('#posterType').value
        if (posterType === 'rating-order') {
            if (globalKey.startsWith('t1-') || globalKey.startsWith('t2-')) {
                setTimeout(function() {
                    page.querySelector('#posterType').value = 'poster-default'
                }, 0);
            } else {
                page.querySelectorAll('.customOrderSettings')[0].style.display = 'block'
            }
        } else {
            page.querySelectorAll('.customOrderSettings')[0].style.display = 'none'
        }
    }

    return function (view, params) {

        view.addEventListener('viewshow', function () {

            loading.show();

            var page = this;

            view.querySelector('.submit-form').addEventListener('click', function(e) {
                onSubmit(e, page)
            });  

            ApiClient.getNamedConfiguration("rpdb").then(function (response) {
                loadPage(page, response, function() {
                    view.querySelector('.load-key').addEventListener('click', function () {
                        onLoadKey(page);
                    });
                    view.querySelector('#txtRpdbApiKey').addEventListener('keyup', function(ev) {
                        if (ev.keyCode === 13) {
                          ev.preventDefault();
                          onLoadKey(page);
                        }
                    })
                    view.querySelector('.button-change-key').addEventListener('click', function (e) {
                        e.preventDefault();
                        globalKey = ''
                        page.querySelectorAll('.invalidKey')[0].style.display = 'none'
                        page.querySelector('#txtRpdbApiKey').value = '';
                        page.querySelectorAll('.rpdbSettings')[0].style.display = 'none'
                        page.querySelectorAll('.load-key')[0].style.display = 'block'
                        page.querySelectorAll('.keyHandler')[0].style.display = 'block'
                    });
                    view.querySelector('#posterType').onchange = function() {
                        onPosterTypeChanged(page);
                    };
                });
            });
        });
    };

});