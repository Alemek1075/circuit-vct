$(function () {
  const $tournament = $('#TournamentId');

  $('[data-team-autocomplete]').each(function () {
    const $input = $(this);
    const $hidden = $('#' + $input.data('hidden'));
    const $list = $('#' + $input.attr('aria-controls'));
    let timer;
    let request;
    let activeIndex = -1;

    function close() {
      $list.empty().prop('hidden', true);
      $input.attr('aria-expanded', 'false').removeAttr('aria-activedescendant');
      activeIndex = -1;
    }

    function choose($option) {
      $hidden.val($option.data('id'));
      $input.val($option.data('name'));
      close();
    }

    function activate(index) {
      const $options = $list.find('[role="option"]');
      if (!$options.length) return;
      activeIndex = (index + $options.length) % $options.length;
      $options.attr('aria-selected', 'false').removeClass('is-active');
      const $option = $options.eq(activeIndex);
      $option.attr('aria-selected', 'true').addClass('is-active');
      $input.attr('aria-activedescendant', $option.attr('id'));
    }

    function showResults(results, term) {
      if ($input.val().trim() !== term) return;
      $list.empty();
      if (!results.length) {
        $('<p class="autocomplete-empty" role="status">Нічого не знайдено</p>').appendTo($list);
      } else {
        results.forEach((team, index) => {
          $('<button type="button" role="option" aria-selected="false">')
            .attr('id', `${$list.attr('id')}-${index}`)
            .data({ id: team.id, name: team.name })
            .text(`${team.name} · ${team.shortName}`)
            .appendTo($list);
        });
      }
      $list.prop('hidden', false);
      $input.attr('aria-expanded', 'true');
    }

    $input.on('input', function () {
      $hidden.val('');
      clearTimeout(timer);
      if (request) request.abort();
      close();
      const term = $input.val().trim();
      const tournamentId = $tournament.val();
      if (term.length < 3 || !tournamentId) return;
      timer = setTimeout(() => {
        request = $.getJSON('/api/teams/search', { q: term, tournamentId })
          .done(results => showResults(results, term))
          .fail((_, status) => {
            if (status === 'abort') return;
            $list.empty().append('<p class="autocomplete-empty" role="status">Пошук недоступний. Спробуйте ще раз.</p>');
            $list.prop('hidden', false);
            $input.attr('aria-expanded', 'true');
          });
      }, 250);
    });

    $input.on('keydown', function (event) {
      if ($list.prop('hidden')) return;
      if (event.key === 'ArrowDown') { event.preventDefault(); activate(activeIndex + 1); }
      if (event.key === 'ArrowUp') { event.preventDefault(); activate(activeIndex - 1); }
      if (event.key === 'Enter' && activeIndex >= 0) {
        event.preventDefault();
        choose($list.find('[role="option"]').eq(activeIndex));
      }
      if (event.key === 'Escape') { event.preventDefault(); close(); }
    });

    $list.on('mousedown', '[role="option"]', function (event) {
      event.preventDefault();
      choose($(this));
    });
    $input.on('blur', () => setTimeout(close, 120));
  });

  $tournament.on('change', function () {
    $('[data-team-autocomplete]').val('').trigger('input');
  });
});
